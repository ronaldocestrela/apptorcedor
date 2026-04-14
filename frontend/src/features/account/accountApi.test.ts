import { describe, expect, it } from 'vitest'
import { resolvePublicAssetUrl } from './accountApi'

describe('resolvePublicAssetUrl', () => {
  it('prefixes API origin for relative paths', () => {
    expect(resolvePublicAssetUrl('/uploads/x.jpg')).toMatch(/\/uploads\/x\.jpg$/)
  })

  it('returns absolute URLs unchanged', () => {
    expect(resolvePublicAssetUrl('https://cdn.example/x.jpg')).toBe('https://cdn.example/x.jpg')
  })
})
