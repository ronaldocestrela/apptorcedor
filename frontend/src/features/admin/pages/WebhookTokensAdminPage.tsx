import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { ShieldAlert, Webhook } from 'lucide-react'
import { PermissionGate } from '../../auth/PermissionGate'
import { useAuth } from '../../auth/AuthContext'
import { ApplicationPermissions } from '../../../shared/auth/applicationPermissions'
import {
  createAdminPartnerApiKey,
  listAdminPartnerApiKeys,
  revokeAdminPartnerApiKey,
  type PartnerApiKeyListItem,
} from '../services/adminApi'
import { Button } from '../../../components/ui/button'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../../../components/ui/tabs'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '../../../components/ui/dialog'
import { Input } from '../../../components/ui/input'
import { Alert, AlertDescription, AlertTitle } from '../../../components/ui/alert'

function formatDate(value: string | null): string {
  if (!value)
    return 'Nunca usado'

  const date = new Date(value)
  if (Number.isNaN(date.getTime()))
    return 'Data inválida'

  return new Intl.DateTimeFormat('pt-BR', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(date)
}

export function WebhookTokensAdminPage() {
  const { user } = useAuth()
  const canManage = useMemo(
    () => (user?.permissions ?? []).includes(ApplicationPermissions.WebhooksGerenciar),
    [user],
  )

  const [items, setItems] = useState<PartnerApiKeyListItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const [createName, setCreateName] = useState('')
  const [creating, setCreating] = useState(false)

  const [createdToken, setCreatedToken] = useState<string | null>(null)
  const [createdTokenName, setCreatedTokenName] = useState<string | null>(null)
  const [copyFeedback, setCopyFeedback] = useState<string | null>(null)

  const [revokeTarget, setRevokeTarget] = useState<PartnerApiKeyListItem | null>(null)
  const [revoking, setRevoking] = useState(false)

  const loadItems = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const rows = await listAdminPartnerApiKeys()
      setItems(rows)
    }
    catch {
      setError('Falha ao carregar tokens de integração.')
      setItems([])
    }
    finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void loadItems()
  }, [loadItems])

  async function onCreate(e: FormEvent) {
    e.preventDefault()
    const name = createName.trim()
    if (!name)
      return

    setCreating(true)
    setError(null)
    setSuccessMessage(null)
    setCopyFeedback(null)

    try {
      const created = await createAdminPartnerApiKey(name)
      setCreateName('')
      setCreatedToken(created.plaintextKey)
      setCreatedTokenName(created.name)
      setSuccessMessage('Token gerado. Salve-o agora, ele não será exibido novamente.')
      await loadItems()
    }
    catch {
      setError('Não foi possível gerar o token.')
    }
    finally {
      setCreating(false)
    }
  }

  async function onRevoke() {
    if (!revokeTarget)
      return

    setRevoking(true)
    setError(null)
    setSuccessMessage(null)
    try {
      await revokeAdminPartnerApiKey(revokeTarget.id)
      setRevokeTarget(null)
      setSuccessMessage(`Token "${revokeTarget.name}" revogado com sucesso.`)
      await loadItems()
    }
    catch {
      setError('Falha ao revogar token.')
    }
    finally {
      setRevoking(false)
    }
  }

  async function copyToken() {
    if (!createdToken)
      return

    try {
      await navigator.clipboard.writeText(createdToken)
      setCopyFeedback('Token copiado para a área de transferência.')
    }
    catch {
      setCopyFeedback('Não foi possível copiar automaticamente. Copie manualmente.')
    }
  }

  return (
    <PermissionGate
      anyOf={[
        ApplicationPermissions.WebhooksVisualizar,
        ApplicationPermissions.WebhooksGerenciar,
      ]}
    >
      <section style={{ textAlign: 'left' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 12 }}>
          <Webhook size={20} color="#8cd392" />
          <h1 style={{ margin: 0 }}>Integrações — Token de API (Partner Lookup)</h1>
        </div>
        <p style={{ marginTop: 0, color: 'var(--admin-text-muted)', maxWidth: 860 }}>
          Este token autentica chamadas externas para o endpoint de consulta por telefone e status de sócio.
          O valor em texto claro só aparece no momento da geração.
        </p>

        {error ? (
          <Alert variant="destructive" style={{ marginBottom: 16 }}>
            <AlertTitle>Erro</AlertTitle>
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        ) : null}

        {successMessage ? (
          <Alert style={{ marginBottom: 16 }}>
            <AlertTitle>Operação concluída</AlertTitle>
            <AlertDescription>{successMessage}</AlertDescription>
          </Alert>
        ) : null}

        <Tabs defaultValue="tokens">
          <TabsList>
            <TabsTrigger value="tokens">Tokens ativos</TabsTrigger>
            <TabsTrigger value="generate" disabled={!canManage}>Gerar token</TabsTrigger>
          </TabsList>

          <TabsContent value="tokens">
            <div style={{ border: '1px solid var(--admin-border-soft)', borderRadius: 12, overflow: 'hidden' }}>
              <div
                style={{
                  display: 'grid',
                  gridTemplateColumns: canManage ? '2fr 1fr 1fr 1fr auto' : '2fr 1fr 1fr 1fr',
                  gap: 8,
                  padding: '12px 14px',
                  borderBottom: '1px solid var(--admin-border-soft)',
                  fontSize: 13,
                  color: 'var(--admin-text-muted)',
                  background: 'rgba(255,255,255,0.02)',
                }}
              >
                <strong>Nome</strong>
                <strong>Prefixo</strong>
                <strong>Criado em</strong>
                <strong>Último uso</strong>
                {canManage ? <strong>Ações</strong> : null}
              </div>

              {loading ? <p style={{ padding: '12px 14px' }}>Carregando tokens...</p> : null}

              {!loading && items.length === 0 ? (
                <p style={{ padding: '12px 14px', color: 'var(--admin-text-muted)' }}>
                  Nenhum token de integração cadastrado.
                </p>
              ) : null}

              {!loading
                ? items.map(item => (
                    <div
                      key={item.id}
                      style={{
                        display: 'grid',
                        gridTemplateColumns: canManage ? '2fr 1fr 1fr 1fr auto' : '2fr 1fr 1fr 1fr',
                        gap: 8,
                        padding: '12px 14px',
                        borderBottom: '1px solid var(--admin-border-soft)',
                        alignItems: 'center',
                        color: 'var(--admin-text-primary)',
                        fontSize: 14,
                      }}
                    >
                      <span>{item.name}</span>
                      <code style={{ color: '#8cd392' }}>{item.keyPrefix}</code>
                      <span>{formatDate(item.createdAt)}</span>
                      <span>{formatDate(item.lastUsedAtUtc)}</span>
                      {canManage ? (
                        <div>
                          <Button
                            variant="destructive"
                            size="sm"
                            onClick={() => setRevokeTarget(item)}
                            disabled={!item.isActive}
                          >
                            Revogar
                          </Button>
                        </div>
                      ) : null}
                    </div>
                  ))
                : null}
            </div>
          </TabsContent>

          <TabsContent value="generate">
            {canManage ? (
              <form
                onSubmit={e => void onCreate(e)}
                style={{
                  border: '1px solid var(--admin-border-soft)',
                  borderRadius: 12,
                  padding: 16,
                  display: 'grid',
                  gap: 12,
                  maxWidth: 560,
                }}
              >
                <label style={{ display: 'grid', gap: 8 }}>
                  <span style={{ color: 'var(--admin-text-muted)', fontSize: 14 }}>Nome do token</span>
                  <Input
                    value={createName}
                    onChange={ev => setCreateName(ev.target.value)}
                    placeholder="Ex.: Loja Parceira XPTO"
                    maxLength={120}
                  />
                </label>

                <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
                  <Button type="submit" disabled={creating || createName.trim().length === 0}>
                    {creating ? 'Gerando...' : 'Gerar token'}
                  </Button>
                </div>
              </form>
            ) : (
              <Alert>
                <ShieldAlert size={16} style={{ marginBottom: 8 }} />
                <AlertTitle>Sem permissão de gerenciamento</AlertTitle>
                <AlertDescription>
                  Seu perfil pode visualizar tokens, mas não pode gerar ou revogar.
                </AlertDescription>
              </Alert>
            )}
          </TabsContent>
        </Tabs>

        <Dialog open={Boolean(createdToken)} onOpenChange={(open) => {
          if (!open) {
            setCreatedToken(null)
            setCreatedTokenName(null)
            setCopyFeedback(null)
          }
        }}>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Token gerado com sucesso</DialogTitle>
              <DialogDescription>
                {createdTokenName ? `Token "${createdTokenName}" criado. ` : ''}
                Este valor é exibido apenas uma vez. Armazene-o agora.
              </DialogDescription>
            </DialogHeader>

            <div style={{ border: '1px dashed rgba(140,211,146,0.4)', borderRadius: 10, padding: 12, wordBreak: 'break-all' }}>
              <code style={{ color: '#8cd392' }}>{createdToken}</code>
            </div>

            {copyFeedback ? <p style={{ color: 'var(--admin-text-muted)', margin: 0 }}>{copyFeedback}</p> : null}

            <DialogFooter>
              <Button variant="secondary" onClick={() => {
                setCreatedToken(null)
                setCreatedTokenName(null)
                setCopyFeedback(null)
              }}>
                Fechar
              </Button>
              <Button onClick={() => void copyToken()}>Copiar token</Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        <Dialog open={Boolean(revokeTarget)} onOpenChange={(open) => {
          if (!open)
            setRevokeTarget(null)
        }}>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Revogar token</DialogTitle>
              <DialogDescription>
                Esta ação é irreversível. O token deixará de autenticar chamadas externas imediatamente.
              </DialogDescription>
            </DialogHeader>

            {revokeTarget ? (
              <div style={{ fontSize: 14, color: 'var(--admin-text-primary)', display: 'grid', gap: 6 }}>
                <span><strong>Nome:</strong> {revokeTarget.name}</span>
                <span><strong>Prefixo:</strong> {revokeTarget.keyPrefix}</span>
                <span><strong>Último uso:</strong> {formatDate(revokeTarget.lastUsedAtUtc)}</span>
              </div>
            ) : null}

            <DialogFooter>
              <Button variant="secondary" onClick={() => setRevokeTarget(null)} disabled={revoking}>
                Cancelar
              </Button>
              <Button variant="destructive" onClick={() => void onRevoke()} disabled={revoking}>
                {revoking ? 'Revogando...' : 'Confirmar revogação'}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </section>
    </PermissionGate>
  )
}
