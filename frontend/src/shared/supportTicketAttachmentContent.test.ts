import { describe, expect, it } from 'vitest'
import { isSupportAttachmentImageContentType } from './supportTicketAttachmentContent'

describe('isSupportAttachmentImageContentType', () => {
  it('returns true for image/*', () => {
    expect(isSupportAttachmentImageContentType('image/png')).toBe(true)
    expect(isSupportAttachmentImageContentType('IMAGE/JPEG')).toBe(true)
    expect(isSupportAttachmentImageContentType('  image/webp  ')).toBe(true)
  })

  it('returns false for pdf and non-images', () => {
    expect(isSupportAttachmentImageContentType('application/pdf')).toBe(false)
    expect(isSupportAttachmentImageContentType('text/plain')).toBe(false)
  })
})
