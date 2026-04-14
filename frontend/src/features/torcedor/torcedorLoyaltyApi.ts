import { api } from '../../shared/api/http'

export type TorcedorLoyaltySummary = {
  totalPoints: number
  monthlyPoints: number
  monthlyRank: number | null
  allTimeRank: number | null
  asOfUtc: string
}

export type TorcedorLoyaltyRankingRow = {
  rank: number
  userId: string
  userName: string
  totalPoints: number
}

export type TorcedorLoyaltyMyStanding = {
  rank: number
  userId: string
  userName: string
  totalPoints: number
}

export type TorcedorLoyaltyRankingPage = {
  totalCount: number
  items: TorcedorLoyaltyRankingRow[]
  me: TorcedorLoyaltyMyStanding | null
}

export async function getMyLoyaltySummary(): Promise<TorcedorLoyaltySummary> {
  const { data } = await api.get<TorcedorLoyaltySummary>('/api/loyalty/me/summary')
  return data
}

export async function getMonthlyLoyaltyRanking(params: {
  year: number
  month: number
  page?: number
  pageSize?: number
}): Promise<TorcedorLoyaltyRankingPage> {
  const { data } = await api.get<TorcedorLoyaltyRankingPage>('/api/loyalty/rankings/monthly', {
    params: {
      year: params.year,
      month: params.month,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
    },
  })
  return data
}

export async function getAllTimeLoyaltyRanking(params?: {
  page?: number
  pageSize?: number
}): Promise<TorcedorLoyaltyRankingPage> {
  const { data } = await api.get<TorcedorLoyaltyRankingPage>('/api/loyalty/rankings/all-time', {
    params: {
      page: params?.page ?? 1,
      pageSize: params?.pageSize ?? 20,
    },
  })
  return data
}
