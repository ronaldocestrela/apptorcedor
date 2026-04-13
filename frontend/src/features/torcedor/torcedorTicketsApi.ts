import { api } from '../../shared/api/http'

export type TorcedorTicketListItem = {
  ticketId: string
  gameId: string
  opponent: string
  competition: string
  gameDate: string
  status: string
  externalTicketId: string | null
  qrCode: string | null
  createdAt: string
  redeemedAt: string | null
}

export type TorcedorTicketListPage = {
  totalCount: number
  items: TorcedorTicketListItem[]
}

export type TorcedorTicketDetail = TorcedorTicketListItem & {
  updatedAt: string
}

export async function listMyTickets(params: {
  gameId?: string
  status?: string
  page?: number
  pageSize?: number
}): Promise<TorcedorTicketListPage> {
  const { data } = await api.get<TorcedorTicketListPage>('/api/tickets', {
    params: {
      gameId: params.gameId || undefined,
      status: params.status || undefined,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return data
}

export async function getMyTicket(ticketId: string): Promise<TorcedorTicketDetail> {
  const { data } = await api.get<TorcedorTicketDetail>(`/api/tickets/${encodeURIComponent(ticketId)}`)
  return data
}

export async function redeemMyTicket(ticketId: string): Promise<void> {
  await api.post(`/api/tickets/${encodeURIComponent(ticketId)}/redeem`)
}
