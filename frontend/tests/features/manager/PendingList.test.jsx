import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import PendingList from '../../../src/features/manager/PendingList.jsx'

const item = (over = {}) => ({
  id: 7, employeeName: 'Lotfi Ferchichi', start: '2027-01-13', end: '2027-01-13',
  workingDays: 1, reason: 'Appointment', flagged: false, flagDetail: null, ...over,
})

describe('PendingList', () => {
  it('shows the team-load warning only for flagged requests', () => {
    render(<PendingList items={[item({ flagged: true, flagDetail: '2 of 4 team members are already away' })]} onDecide={vi.fn()} />)
    expect(screen.getByRole('note')).toHaveTextContent('2 of 4')
  })

  it('requires a comment before rejecting', async () => {
    const user = userEvent.setup()
    const onDecide = vi.fn().mockResolvedValue()
    render(<PendingList items={[item()]} onDecide={onDecide} />)
    expect(screen.getByRole('button', { name: 'Reject' })).toBeDisabled()
    await user.type(screen.getByLabelText(/Comment/), 'Release week')
    await user.click(screen.getByRole('button', { name: 'Reject' }))
    expect(onDecide).toHaveBeenCalledWith(7, false, 'Release week')
  })

  it('approves without a comment', async () => {
    const user = userEvent.setup()
    const onDecide = vi.fn().mockResolvedValue()
    render(<PendingList items={[item()]} onDecide={onDecide} />)
    await user.click(screen.getByRole('button', { name: 'Approve' }))
    expect(onDecide).toHaveBeenCalledWith(7, true, '')
  })

  it('shows an empty state', () => {
    render(<PendingList items={[]} onDecide={vi.fn()} />)
    expect(screen.getByText('Nothing to review right now.')).toBeInTheDocument()
  })
})
