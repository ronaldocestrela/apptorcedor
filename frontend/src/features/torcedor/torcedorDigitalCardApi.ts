import { api } from '../../shared/api/http'

/** Alinhado a `MyDigitalCardViewDto` da API (C.3). */
export type MyDigitalCardViewState =
  | 'NotAssociated'
  | 'MembershipInactive'
  | 'AwaitingIssuance'
  | 'Active'

export type MyDigitalCardView = {
  state: MyDigitalCardViewState
  membershipStatus: string
  message: string | null
  membershipId: string | null
  digitalCardId: string | null
  version: number | null
  cardStatus: string | null
  issuedAt: string | null
  verificationToken: string | null
  templatePreviewLines: string[] | null
  cacheValidUntilUtc: string | null
}

export const DIGITAL_CARD_LOCAL_STORAGE_KEY = 'appTorcedor.digitalCard.cache.v1'

export function readDigitalCardCache(): MyDigitalCardView | null {
  try {
    const raw = localStorage.getItem(DIGITAL_CARD_LOCAL_STORAGE_KEY)
    if (!raw)
      return null
    const parsed = JSON.parse(raw) as MyDigitalCardView
    if (!parsed?.cacheValidUntilUtc)
      return null
    if (Date.parse(parsed.cacheValidUntilUtc) <= Date.now())
      return null
    return parsed
  }
  catch {
    return null
  }
}

function writeDigitalCardCache(data: MyDigitalCardView) {
  try {
    if (data.cacheValidUntilUtc && Date.parse(data.cacheValidUntilUtc) > Date.now())
      localStorage.setItem(DIGITAL_CARD_LOCAL_STORAGE_KEY, JSON.stringify(data))
  }
  catch {
    /* storage cheio / desativado */
  }
}

export type GetMyDigitalCardOptions = {
  /** Se a rede falhar, usa cache local ainda válido (`cacheValidUntilUtc`). */
  allowStaleOnNetworkError?: boolean
}

export type GetMyDigitalCardResult = {
  data: MyDigitalCardView
  /** `true` quando a rede falhou e os dados vieram do armazenamento local (C.3 offline limitado). */
  fromCache: boolean
}

export async function getMyDigitalCardWithSource(options?: GetMyDigitalCardOptions): Promise<GetMyDigitalCardResult> {
  try {
    const { data } = await api.get<MyDigitalCardView>('/api/account/digital-card')
    writeDigitalCardCache(data)
    return { data, fromCache: false }
  }
  catch (e) {
    if (options?.allowStaleOnNetworkError) {
      const cached = readDigitalCardCache()
      if (cached)
        return { data: cached, fromCache: true }
    }
    throw e
  }
}

export async function getMyDigitalCard(options?: GetMyDigitalCardOptions): Promise<MyDigitalCardView> {
  const r = await getMyDigitalCardWithSource(options)
  return r.data
}

/** Apenas para testes. */
export function clearDigitalCardCacheForTests() {
  localStorage.removeItem(DIGITAL_CARD_LOCAL_STORAGE_KEY)
}
