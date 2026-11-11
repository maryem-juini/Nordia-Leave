import { useCallback, useEffect, useState } from 'react'
import { api, setCurrentUser } from '../services/api.js'
import BalanceCard from '../features/employee/BalanceCard.jsx'
import LeaveForm from '../features/employee/LeaveForm.jsx'
import History from '../features/employee/History.jsx'
import PendingList from '../features/manager/PendingList.jsx'

const YEARS = [2026, 2027, 2028]

export default function App() {
  const [employees, setEmployees] = useState([])
  const [userId, setUserId] = useState(null)
  const [year, setYear] = useState(2027)
  const [balance, setBalance] = useState(null)
  const [leaves, setLeaves] = useState([])
  const [pending, setPending] = useState([])
  const [error, setError] = useState('')

  const user = employees.find((e) => e.id === userId)
  const isManager = user?.role === 'manager'

  useEffect(() => {
    api.employees()
      .then((list) => {
        setEmployees(list)
        setUserId(list[0]?.id ?? null)
      })
      .catch(() => setError('Cannot reach the API. Is the .NET project running on http://localhost:5080 ?'))
  }, [])

  const refresh = useCallback(async () => {
    if (!user) return
    setCurrentUser(user.id)
    setError('')
    try {
      if (user.role === 'manager') {
        setBalance(null)
        setLeaves([])
        setPending(await api.pending())
      } else {
        const [b, l] = await Promise.all([api.balance(year), api.myLeaves()])
        setBalance(b)
        setLeaves(l)
        setPending([])
      }
    } catch (err) {
      setError(err.message)
    }
  }, [user, year])

  useEffect(() => { refresh() }, [refresh])

  async function createLeave(body) {
    setCurrentUser(user.id)
    await api.create(body)
    await refresh()
  }

  async function decide(id, approve, comment) {
    setCurrentUser(user.id)
    await api.decide(id, approve, comment)
    await refresh()
  }

  const teams = [...new Set(employees.map((e) => e.team))]

  return (
    <div className="app">
      <header>
        <div className="brand">
          <p className="eyebrow">People operations</p>
          <h1>Nordia <span>Leave</span></h1>
        </div>
        <div className="switch">
          <label>
            Viewing as
            <select value={userId ?? ''} onChange={(e) => setUserId(Number(e.target.value))} aria-label="Viewing as">
              {teams.map((t) => (
                <optgroup key={t} label={t}>
                  {employees.filter((e) => e.team === t).map((e) => (
                    <option key={e.id} value={e.id}>{e.firstName} {e.lastName} ({e.role})</option>
                  ))}
                </optgroup>
              ))}
            </select>
          </label>
          <label>
            Year
            <select value={year} onChange={(e) => setYear(Number(e.target.value))}>
              {YEARS.map((y) => <option key={y}>{y}</option>)}
            </select>
          </label>
        </div>
      </header>

      {error && <p className="error banner" role="alert">{error}</p>}

      {user && (isManager ? (
        <main>
          <PendingList items={pending} onDecide={decide} />
        </main>
      ) : (
        <main className="grid">
          <div>
            <BalanceCard balance={balance} />
            <LeaveForm remaining={balance?.remaining ?? 0} onSubmit={createLeave} />
          </div>
          <History leaves={leaves} />
        </main>
      ))}
    </div>
  )
}
