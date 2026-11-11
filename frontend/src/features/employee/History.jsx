import { formatRange } from '../../shared/dates.js'

export default function History({ leaves }) {
  return (
    <section className="panel" aria-label="My requests">
      <h2>My requests</h2>
      {leaves.length === 0 ? (
        <p className="muted">No request yet. Send your first one with the form.</p>
      ) : (
        <table>
          <thead>
            <tr><th>Dates</th><th>Days</th><th>Status</th><th>Details</th></tr>
          </thead>
          <tbody>
            {leaves.map((l) => (
              <tr key={l.id}>
                <td>{formatRange(l.start, l.end)}</td>
                <td>{l.workingDays}</td>
                <td><span className={`badge ${l.status}`}>{l.status}</span></td>
                <td>
                  {l.reason}
                  {l.managerComment && <div className="muted">Manager: {l.managerComment}</div>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  )
}
