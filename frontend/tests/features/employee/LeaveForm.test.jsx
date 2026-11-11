import { render, screen, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import LeaveForm from '../../../src/features/employee/LeaveForm.jsx'

const fill = async () => {
  fireEvent.change(screen.getByLabelText('First day'), { target: { value: '2027-03-01' } })
  fireEvent.change(screen.getByLabelText('Last day'), { target: { value: '2027-03-05' } })
}

describe('LeaveForm', () => {
  it('keeps the last day separate when a first day is selected', () => {
    render(<LeaveForm remaining={20} onSubmit={vi.fn()} />)
    fireEvent.change(screen.getByLabelText('First day'), { target: { value: '2027-03-01' } })
    expect(screen.getByLabelText('Last day')).toHaveValue('')
    expect(screen.getByRole('status')).toHaveTextContent('Pick your dates')
    expect(screen.getByRole('button', { name: 'Send request' })).toBeDisabled()
  })

  it('shows a live working-day count and submits the values', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn().mockResolvedValue()
    render(<LeaveForm remaining={20} onSubmit={onSubmit} />)
    await fill()
    expect(screen.getByRole('status')).toHaveTextContent('5 working days')
    await user.click(screen.getByRole('button', { name: 'Send request' }))
    expect(onSubmit).toHaveBeenCalledWith({ start: '2027-03-01', end: '2027-03-05', reason: '' })
  })

  it('blocks a request larger than the remaining balance', async () => {
    const user = userEvent.setup()
    render(<LeaveForm remaining={3} onSubmit={vi.fn()} />)
    await fill()
    expect(screen.getByRole('status')).toHaveTextContent('more than your remaining balance')
    expect(screen.getByRole('button', { name: 'Send request' })).toBeDisabled()
  })

  it('blocks a request with dates that are not in the future', () => {
    render(<LeaveForm remaining={20} onSubmit={vi.fn()} />)
    fireEvent.change(screen.getByLabelText('First day'), { target: { value: '2020-03-02' } })
    fireEvent.change(screen.getByLabelText('Last day'), { target: { value: '2020-03-06' } })

    expect(screen.getByRole('status')).toHaveTextContent('Choose dates in the future')
    expect(screen.getByRole('button', { name: 'Send request' })).toBeDisabled()
  })

  it('displays the API error when the server refuses the request', async () => {
    const user = userEvent.setup()
    const onSubmit = vi.fn().mockRejectedValue(new Error('This request overlaps your leave'))
    render(<LeaveForm remaining={20} onSubmit={onSubmit} />)
    await fill()
    await user.click(screen.getByRole('button', { name: 'Send request' }))
    expect(await screen.findByRole('alert')).toHaveTextContent('overlaps')
  })
})
