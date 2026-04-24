import axios from 'axios'
import { describe, expect, it } from 'vitest'
import { formatRegisterApiErrorMessage } from './accountApi'

describe('formatRegisterApiErrorMessage', () => {
  it('returns fallback for non-axios errors', () => {
    expect(formatRegisterApiErrorMessage(new Error('x'), 'fallback')).toBe('fallback')
  })

  it('joins errors array from 400 response body', () => {
    const err = new axios.AxiosError('Bad Request')
    err.response = {
      status: 400,
      data: { errors: ['First.', 'Second.'] },
      statusText: 'Bad Request',
      headers: {},
      config: {} as never,
    }
    expect(formatRegisterApiErrorMessage(err, 'fallback')).toBe('First. Second.')
  })

  it('returns fallback when errors missing or empty', () => {
    const err = new axios.AxiosError('Bad Request')
    err.response = {
      status: 400,
      data: { errors: [] },
      statusText: 'Bad Request',
      headers: {},
      config: {} as never,
    }
    expect(formatRegisterApiErrorMessage(err, 'fallback')).toBe('fallback')
  })
})
