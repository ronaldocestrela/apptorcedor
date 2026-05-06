export type ViaCepLookupResult = {
  cepDigits: string
  street: string
  neighborhood: string
  city: string
  state: string
}

/** Returns empty string if input has no digits. */
export function cepDigitsOnly(raw: string): string {
  return raw.replace(/\D/g, '')
}

/**
 * ViaCEP JSON. `erro: true` means CEP not found.
 * @see https://viacep.com.br/
 */
export async function lookupViaCep(cepDigits: string): Promise<ViaCepLookupResult | null> {
  const clean = cepDigitsOnly(cepDigits)
  if (clean.length !== 8)
    return null

  const res = await fetch(`https://viacep.com.br/ws/${clean}/json/`)
  if (!res.ok)
    throw new Error('viacep_http_error')

  const data = (await res.json()) as {
    erro?: boolean
    logradouro?: string
    bairro?: string
    localidade?: string
    uf?: string
  }

  if (data.erro === true)
    return null

  return {
    cepDigits: clean,
    street: (data.logradouro ?? '').trim(),
    neighborhood: (data.bairro ?? '').trim(),
    city: (data.localidade ?? '').trim(),
    state: (data.uf ?? '').trim().toUpperCase(),
  }
}
