import { describe, expect, it } from 'vitest'
import {
  evaluatePublicRegisterPasswordRules,
  publicRegisterPasswordMeetsAllRules,
} from './registerPasswordRules'

describe('registerPasswordRules', () => {
  it('rejects password shorter than 8 chars', () => {
    expect(publicRegisterPasswordMeetsAllRules('Ab1')).toBe(false)
    const rules = evaluatePublicRegisterPasswordRules('Ab1')
    expect(rules.find((r) => r.id === 'minLength')?.met).toBe(false)
  })

  it('rejects when missing uppercase', () => {
    expect(publicRegisterPasswordMeetsAllRules('abcdefgh1')).toBe(false)
    expect(evaluatePublicRegisterPasswordRules('abcdefgh1').find((r) => r.id === 'uppercase')?.met).toBe(
      false,
    )
  })

  it('rejects when missing lowercase', () => {
    expect(publicRegisterPasswordMeetsAllRules('ABCDEFGH1')).toBe(false)
    expect(evaluatePublicRegisterPasswordRules('ABCDEFGH1').find((r) => r.id === 'lowercase')?.met).toBe(
      false,
    )
  })

  it('rejects when missing digit', () => {
    expect(publicRegisterPasswordMeetsAllRules('Abcdefgh')).toBe(false)
    expect(evaluatePublicRegisterPasswordRules('Abcdefgh').find((r) => r.id === 'digit')?.met).toBe(false)
  })

  it('accepts valid password without special char', () => {
    expect(publicRegisterPasswordMeetsAllRules('Abcdefg1')).toBe(true)
    expect(evaluatePublicRegisterPasswordRules('Abcdefg1').every((r) => r.met)).toBe(true)
  })
})
