export default function BalanceCard({ balance }) {
  if (!balance) return null
  const used = balance.approved + balance.pending
  const pct = Math.min(100, Math.round((used / balance.allowance) * 100))
  return (
    <section className="panel" aria-label="Balance">
      <h2>Balance {balance.year}</h2>
      <p className="remaining"><strong>{balance.remaining}</strong> of {balance.allowance} working days left</p>
      <div className="bar" role="img" aria-label={`${pct}% used`}><div style={{ width: `${pct}%` }} /></div>
      <p className="muted">{balance.approved} approved · {balance.pending} awaiting approval</p>
    </section>
  )
}
