let currentUserId = null

export function setCurrentUser(id) {
  currentUserId = id
}

async function request(path, options = {}) {
  const res = await fetch(`/api${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      'X-Employee-Id': String(currentUserId ?? ''),
    },
  })
  const data = await res.json().catch(() => null)
  if (!res.ok) {
    throw new Error(data?.error ?? data?.title ?? `Request failed (${res.status})`)
  }
  return data
}

export const api = {
  employees: () => request('/employees'),
  balance: (year) => request(`/me/balance?year=${year}`),
  myLeaves: () => request('/me/leaves'),
  create: (body) => request('/leaves', { method: 'POST', body: JSON.stringify(body) }),
  pending: () => request('/leaves/pending'),
  decide: (id, approve, comment) =>
    request(`/leaves/${id}/decision`, { method: 'POST', body: JSON.stringify({ approve, comment }) }),
}
