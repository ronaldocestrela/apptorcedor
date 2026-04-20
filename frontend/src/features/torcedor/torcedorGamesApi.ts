import { api } from '../../shared/api/http'

export type TorcedorGameListItem = {
  gameId: string
  opponent: string
  competition: string
  gameDate: string
  createdAt: string
}

export type TorcedorGameListPage = {
  totalCount: number
  items: TorcedorGameListItem[]
}

export async function listTorcedorGames(params: {
  search?: string
  page?: number
  pageSize?: number
}): Promise<TorcedorGameListPage> {
  const { data } = await api.get<TorcedorGameListPage>('/api/games', {
    params: {
      search: params.search || undefined,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return data
}
