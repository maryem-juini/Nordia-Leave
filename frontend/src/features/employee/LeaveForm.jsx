import { useState } from 'react'
import { countWorkingDays, getTomorrowDate } from '../../shared/dates.js'

export default function LeaveForm({ remaining, onSubmit }) {
  const earliestDate = getTomorrowDate()
  const [start, setStart] = useState('')
  const [end, setEnd] = useState('')
  const [reason, setReason] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  const days = countWorkingDays(start, end)
  const tooLong = days > remaining
  const datesInFuture = start >= earliestDate && end >= earliestDate
  const canSubmit = days > 0 && datesInFuture && !tooLong && !busy

  async function submit(e) {
    e.preventDefault()
    setError('')
    setBusy(true)
    try {
      await onSubmit({ start, end, reason })
      setStart('')
      setEnd('')
      setReason('')
    } catch (err) {
      setError(err.message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <form onSubmit={submit} className="panel form" aria-label="New leave request">
      <h2>Request leave</h2>
      <div className="row">
        <label>
          First day
          <input type="date" value={start} min={earliestDate} onChange={(e) => { setStart(e.target.value); if (end && e.target.value > end) setEnd('') }} required />
        </label>
        <label>
          Last day
          <input type="date" value={end} min={start || earliestDate} onChange={(e) => setEnd(e.target.value)} required />
        </label>
      </div>
      <label>
        Reason (optional)
        <input type="text" maxLength={500} value={reason} onChange={(e) => setReason(e.target.value)} />
      </label>
      <p className={tooLong ? 'hint bad' : 'hint'} role="status">
        {start && end && !datesInFuture ? 'Choose dates in the future' : days > 0 ? `${days} working day${days > 1 ? 's' : ''}` : 'Pick your dates'}
        {tooLong ? ` — more than your remaining balance (${remaining})` : ''}
      </p>
      {error && <p className="error" role="alert">{error}</p>}
      <button type="submit" disabled={!canSubmit}>Send request</button>
    </form>
  )
}
