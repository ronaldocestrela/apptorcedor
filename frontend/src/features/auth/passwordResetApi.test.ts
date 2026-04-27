import { describe, expect, it } from 'vitest'
import { AxiosError } from 'axios'
import { formatResetPasswordApiErrorMessage } from './passwordResetApi'

describe('formatResetPasswordApiErrorMessage', () => {
  it('joins errors from API body', () => {
    const err = new AxiosError('bad', 'ERR', undefined, undefined, {
      status: 400,
      data: { errors: ['a', 'b'] },
    } as never)
    expect(formatResetPasswordApiErrorMessage(err)).toBe('a b')
  })

  it('returns default when not axios', () => {
    expect(formatResetPasswordApiErrorMessage(new Error('x'))).toMatch(/Não foi possível/)
  })
})
