import { useState } from 'react'
import { formatRange } from '../../shared/dates.js'

function PendingCard({ item, onDecide }) {
  const [comment, setComment] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  async function decide(approve) {
    setError('')
    setBusy(true)
    try {
      await onDecide(item.id, approve, comment)
    } catch (err) {
      setError(err.message)
      setBusy(false)
    }
  }

  return (
    <li className={item.flagged ? 'pending flagged' : 'pending'}>
      <div className="pending-head">
        <strong>{item.employeeName}</strong>
        <span>{formatRange(item.start, item.end)} · {item.workingDays} working day{item.workingDays > 1 ? 's' : ''}</span>
      </div>
      {item.reason && <p>{item.reason}</p>}
      {item.flagged && <p className="flag" role="note">⚠ Team load: {item.flagDetail}</p>}
      <label>
        Comment (required to reject)
        <input type="text" value={comment} maxLength={500} onChange={(e) => setComment(e.target.value)} />
      </label>
      {error && <p className="error" role="alert">{error}</p>}
      <div className="actions">
        <button disabled={busy} onClick={() => decide(true)}>Approve</button>
        <button className="danger" disabled={busy || comment.trim() === ''} onClick={() => decide(false)}>Reject</button>
      </div>
    </li>
  )
}

export default function PendingList({ items, onDecide }) {
  return (
    <section className="panel" aria-label="Pending requests">
      <h2>Pending requests from your team</h2>
      {items.length === 0 ? (
        <p className="muted">Nothing to review right now.</p>
      ) : (
        <ul className="plain">
          {items.map((i) => <PendingCard key={i.id} item={i} onDecide={onDecide} />)}
        </ul>
      )}
    </section>
  )
}
