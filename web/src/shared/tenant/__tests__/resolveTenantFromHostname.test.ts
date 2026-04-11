import { describe, expect, it } from 'vitest'
import { resolveTenantFromHostname } from '../resolveTenantFromHostname'

describe('resolveTenantFromHostname', () => {
  it('returns slug for valid multi-label host', () => {
    expect(resolveTenantFromHostname('feira.example.com')).toEqual({ ok: true, slug: 'feira' })
  })

  it('normalizes case', () => {
    expect(resolveTenantFromHostname('Feira.EXAMPLE.com')).toEqual({ ok: true, slug: 'feira' })
  })

  it('rejects empty', () => {
    expect(resolveTenantFromHostname('')).toEqual({ ok: false, reason: 'empty_host' })
  })

  it('rejects localhost', () => {
    expect(resolveTenantFromHostname('localhost')).toEqual({ ok: false, reason: 'localhost_or_ip' })
  })

  it('rejects ipv4', () => {
    expect(resolveTenantFromHostname('127.0.0.1')).toEqual({ ok: false, reason: 'localhost_or_ip' })
  })

  it('uses first DNS label as slug when host has TLD (e.g. club.example.com)', () => {
    expect(resolveTenantFromHostname('club.example.com')).toEqual({ ok: true, slug: 'club' })
  })

  it('rejects www', () => {
    expect(resolveTenantFromHostname('www.example.com')).toEqual({ ok: false, reason: 'www_reserved' })
  })
})
