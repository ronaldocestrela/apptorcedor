/**
 * Heuristic for support ticket message attachments. Backend only allows
 * image/jpeg, image/png, image/webp and application/pdf.
 */
export function isSupportAttachmentImageContentType(contentType: string): boolean {
  const t = contentType.trim().toLowerCase()
  return t.startsWith('image/')
}
