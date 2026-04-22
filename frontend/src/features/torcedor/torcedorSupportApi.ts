import { api } from '../../shared/api/http'

export type TorcedorSupportAttachment = {
  attachmentId: string
  fileName: string
  contentType: string
  downloadUrl: string
}

export type TorcedorSupportMessage = {
  messageId: string
  authorUserId: string
  body: string
  createdAtUtc: string
  attachments: TorcedorSupportAttachment[]
}

export type TorcedorSupportHistoryEntry = {
  entryId: string
  eventType: string
  fromValue: string | null
  toValue: string | null
  actorUserId: string
  reason: string | null
  createdAtUtc: string
}

export type TorcedorSupportListItem = {
  ticketId: string
  queue: string
  subject: string
  priority: string
  status: string
  slaDeadlineUtc: string
  isSlaBreached: boolean
  firstResponseAtUtc: string | null
  createdAtUtc: string
  updatedAtUtc: string
}

export type TorcedorSupportListPage = {
  totalCount: number
  items: TorcedorSupportListItem[]
}

export type TorcedorSupportDetail = {
  ticketId: string
  queue: string
  subject: string
  priority: string
  status: string
  slaDeadlineUtc: string
  isSlaBreached: boolean
  firstResponseAtUtc: string | null
  createdAtUtc: string
  updatedAtUtc: string
  messages: TorcedorSupportMessage[]
  history: TorcedorSupportHistoryEntry[]
}

export async function listMySupportTickets(params: {
  status?: string
  page?: number
  pageSize?: number
}): Promise<TorcedorSupportListPage> {
  const { data } = await api.get<TorcedorSupportListPage>('/api/support/tickets', {
    params: {
      status: params.status || undefined,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return data
}

export async function getMySupportTicket(ticketId: string): Promise<TorcedorSupportDetail> {
  const { data } = await api.get<TorcedorSupportDetail>(
    `/api/support/tickets/${encodeURIComponent(ticketId)}`,
  )
  return data
}

export async function createMySupportTicket(input: {
  queue: string
  subject: string
  priority: string
  initialMessage?: string
  files?: File[]
}): Promise<{ ticketId: string }> {
  const form = new FormData()
  form.append('queue', input.queue)
  form.append('subject', input.subject)
  form.append('priority', input.priority)
  if (input.initialMessage)
    form.append('initialMessage', input.initialMessage)
  for (const f of input.files ?? [])
    form.append('attachments', f)

  const { data } = await api.post<{ ticketId: string }>('/api/support/tickets', form, {
    transformRequest: (body, headers) => {
      if (body instanceof FormData)
        delete headers['Content-Type']
      return body
    },
  })
  return data
}

export async function replyMySupportTicket(ticketId: string, input: { body?: string; files?: File[] }): Promise<void> {
  const form = new FormData()
  if (input.body)
    form.append('body', input.body)
  for (const f of input.files ?? [])
    form.append('attachments', f)

  await api.post(`/api/support/tickets/${encodeURIComponent(ticketId)}/reply`, form, {
    transformRequest: (body, headers) => {
      if (body instanceof FormData)
        delete headers['Content-Type']
      return body
    },
  })
}

export async function cancelMySupportTicket(ticketId: string): Promise<void> {
  await api.post(`/api/support/tickets/${encodeURIComponent(ticketId)}/cancel`)
}

export async function reopenMySupportTicket(ticketId: string): Promise<void> {
  await api.post(`/api/support/tickets/${encodeURIComponent(ticketId)}/reopen`)
}

/** GET with JWT; response body as Blob (e.g. preview). Paths like /api/support/tickets/.../attachments/... */
export async function fetchMySupportAttachmentBlob(downloadPath: string): Promise<Blob> {
  const res = await api.get<Blob>(downloadPath, { responseType: 'blob' })
  return res.data
}

/** GET with JWT; triggers browser download (paths from API are like /api/support/tickets/.../attachments/...). */
export async function downloadMySupportAttachment(downloadPath: string, fileName: string): Promise<void> {
  const blob = await fetchMySupportAttachmentBlob(downloadPath)
  const blobUrl = URL.createObjectURL(blob)
  try {
    const a = document.createElement('a')
    a.href = blobUrl
    a.download = fileName
    a.rel = 'noopener'
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
  }
  finally {
    URL.revokeObjectURL(blobUrl)
  }
}
