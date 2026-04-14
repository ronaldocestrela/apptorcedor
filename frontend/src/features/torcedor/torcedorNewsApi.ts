import { api } from '../../shared/api/http'

export type TorcedorNewsFeedItem = {
  newsId: string
  title: string
  summary: string | null
  publishedAt: string
  updatedAt: string
}

export type TorcedorNewsFeedPage = {
  totalCount: number
  items: TorcedorNewsFeedItem[]
}

export type TorcedorNewsDetail = {
  newsId: string
  title: string
  summary: string | null
  content: string
  publishedAt: string
  updatedAt: string
}

export async function listTorcedorNewsFeed(params: {
  search?: string
  page?: number
  pageSize?: number
}): Promise<TorcedorNewsFeedPage> {
  const { data } = await api.get<TorcedorNewsFeedPage>('/api/news', {
    params: {
      search: params.search || undefined,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return data
}

export async function getTorcedorNewsDetail(newsId: string): Promise<TorcedorNewsDetail> {
  const { data } = await api.get<TorcedorNewsDetail>(`/api/news/${encodeURIComponent(newsId)}`)
  return data
}
