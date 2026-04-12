import { describe, expect, it } from 'vitest'
import { isStripeDirectProvider, normalizeMemberGatewayStatus } from '../memberGatewayStatus'

describe('normalizeMemberGatewayStatus', () => {
  it('maps camelCase from API', () => {
    expect(
      normalizeMemberGatewayStatus({
        selectedProvider: 'StripeDirect',
        status: 'Ready',
        publishableKeyHint: 'pk_live…',
        webhookSecretConfigured: true,
      }),
    ).toEqual({
      selectedProvider: 'StripeDirect',
      status: 'Ready',
      publishableKeyHint: 'pk_live…',
      webhookSecretConfigured: true,
    })
  })

  it('maps PascalCase payload', () => {
    expect(
      normalizeMemberGatewayStatus({
        SelectedProvider: 'StripeDirect',
        Status: 'NotConfigured',
        PublishableKeyHint: null,
        WebhookSecretConfigured: false,
      }),
    ).toEqual({
      selectedProvider: 'StripeDirect',
      status: 'NotConfigured',
      publishableKeyHint: null,
      webhookSecretConfigured: false,
    })
  })

  it('defaults missing provider to None', () => {
    expect(normalizeMemberGatewayStatus({})).toEqual({
      selectedProvider: 'None',
      status: 'Unknown',
      publishableKeyHint: null,
      webhookSecretConfigured: false,
    })
  })
})

describe('isStripeDirectProvider', () => {
  it('accepts casing variants', () => {
    expect(isStripeDirectProvider('StripeDirect')).toBe(true)
    expect(isStripeDirectProvider('stripedirect')).toBe(true)
    expect(isStripeDirectProvider(' None ')).toBe(false)
  })
})
