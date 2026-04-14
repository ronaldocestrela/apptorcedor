import { api } from '../../../shared/api/http'

export type LegalDocumentType = 'TermsOfUse' | 'PrivacyPolicy'

export type LegalDocumentListItem = {
  id: string
  type: LegalDocumentType
  title: string
  createdAt: string
  publishedVersionNumber: number | null
  publishedVersionId: string | null
}

export type LegalDocumentVersionDetail = {
  id: string
  legalDocumentId: string
  versionNumber: number
  content: string
  status: string
  publishedAt: string | null
  createdAt: string
}

export type LegalDocumentDetail = {
  id: string
  type: LegalDocumentType
  title: string
  createdAt: string
  versions: LegalDocumentVersionDetail[]
}

export type UserConsentRow = {
  id: string
  userId: string
  legalDocumentVersionId: string
  documentVersionNumber: number
  documentType: LegalDocumentType
  documentTitle: string
  acceptedAt: string
  clientIp: string | null
}

export type PrivacyOperationResult = {
  requestId: string
  kind: string
  status: string
  resultJson: string | null
  errorMessage: string | null
  createdAt: string
  completedAt: string | null
}

export async function listLegalDocuments(): Promise<LegalDocumentListItem[]> {
  const { data } = await api.get<LegalDocumentListItem[]>('/api/admin/lgpd/documents')
  return data
}

export async function getLegalDocument(id: string): Promise<LegalDocumentDetail> {
  const { data } = await api.get<LegalDocumentDetail>(`/api/admin/lgpd/documents/${encodeURIComponent(id)}`)
  return data
}

export async function createLegalDocument(body: { type: LegalDocumentType; title: string }): Promise<LegalDocumentDetail> {
  const { data } = await api.post<LegalDocumentDetail>('/api/admin/lgpd/documents', body)
  return data
}

export async function addLegalDocumentVersion(documentId: string, content: string): Promise<LegalDocumentVersionDetail> {
  const { data } = await api.post<LegalDocumentVersionDetail>(
    `/api/admin/lgpd/documents/${encodeURIComponent(documentId)}/versions`,
    { content },
  )
  return data
}

export async function publishLegalDocumentVersion(versionId: string): Promise<void> {
  await api.post(`/api/admin/lgpd/legal-document-versions/${encodeURIComponent(versionId)}/publish`)
}

export async function listUserConsents(userId: string): Promise<UserConsentRow[]> {
  const { data } = await api.get<UserConsentRow[]>(`/api/admin/lgpd/users/${encodeURIComponent(userId)}/consents`)
  return data
}

export async function recordUserConsent(userId: string, documentVersionId: string, clientIp?: string | null): Promise<void> {
  await api.post(`/api/admin/lgpd/users/${encodeURIComponent(userId)}/consents`, {
    documentVersionId,
    clientIp: clientIp ?? null,
  })
}

export async function exportUserData(userId: string): Promise<PrivacyOperationResult> {
  const { data } = await api.post<PrivacyOperationResult>(`/api/admin/lgpd/users/${encodeURIComponent(userId)}/export`)
  return data
}

export async function anonymizeUser(userId: string): Promise<PrivacyOperationResult> {
  const { data } = await api.post<PrivacyOperationResult>(`/api/admin/lgpd/users/${encodeURIComponent(userId)}/anonymize`)
  return data
}
