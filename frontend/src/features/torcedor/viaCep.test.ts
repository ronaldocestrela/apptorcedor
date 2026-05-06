import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest'
import { cepDigitsOnly, lookupViaCep } from './viaCep'

describe('viaCep', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn())
  })
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('cepDigitsOnly removes non-digits', () => {
    expect(cepDigitsOnly('01310-100')).toBe('01310100')
  })

  it('lookupViaCep returns null when CEP length is not 8', async () => {
    await expect(lookupViaCep('123')).resolves.toBeNull()
    expect(fetch).not.toHaveBeenCalled()
  })

  it('lookupViaCep maps ViaCEP JSON to fields', async () => {
    vi.mocked(fetch).mockResolvedValue(
      new Response(
        JSON.stringify({
          logradouro: 'Av Paulista',
          bairro: 'Bela Vista',
          localidade: 'São Paulo',
          uf: 'SP',
        }),
        { status: 200 },
      ),
    )
    const r = await lookupViaCep('01310100')
    expect(r).toEqual({
      cepDigits: '01310100',
      street: 'Av Paulista',
      neighborhood: 'Bela Vista',
      city: 'São Paulo',
      state: 'SP',
    })
    expect(fetch).toHaveBeenCalledWith('https://viacep.com.br/ws/01310100/json/')
  })

  it('lookupViaCep returns null when erro true', async () => {
    vi.mocked(fetch).mockResolvedValue(
      new Response(JSON.stringify({ erro: true }), { status: 200 }),
    )
    await expect(lookupViaCep('00000000')).resolves.toBeNull()
  })

  it('lookupViaCep throws on HTTP error', async () => {
    vi.mocked(fetch).mockResolvedValue(new Response('', { status: 500 }))
    await expect(lookupViaCep('01310100')).rejects.toThrow('viacep_http_error')
  })
})
