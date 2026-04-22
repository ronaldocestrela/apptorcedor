import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { SupportTicketAttachmentList } from './SupportTicketAttachmentList'

describe('SupportTicketAttachmentList', () => {
  const createUrl = vi.fn((b: Blob) => `blob:mock/${b.size}`)
  const revokeUrl = vi.fn()

  beforeEach(() => {
    vi.stubGlobal('URL', {
      ...URL,
      createObjectURL: createUrl,
      revokeObjectURL: revokeUrl,
    } as unknown as typeof URL)
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.clearAllMocks()
  })

  it('renders non-image as download control', async () => {
    const downloadFile = vi.fn().mockResolvedValue(undefined)
    const fetchBlob = vi.fn()
    const user = userEvent.setup()
    render(
      <SupportTicketAttachmentList
        fetchBlob={fetchBlob}
        downloadFile={downloadFile}
        attachments={[
          {
            id: '1',
            fileName: 'doc.pdf',
            contentType: 'application/pdf',
            downloadPath: '/api/x/1',
          },
        ]}
      />,
    )
    const btn = screen.getByRole('button', { name: 'doc.pdf' })
    await user.click(btn)
    expect(fetchBlob).not.toHaveBeenCalled()
    expect(downloadFile).toHaveBeenCalledWith('/api/x/1', 'doc.pdf')
  })

  it('loads image thumbnail, opens modal, closes on Fechar', async () => {
    const downloadFile = vi.fn().mockResolvedValue(undefined)
    const png = new Uint8Array([0x89, 0x50, 0x4e, 0x47]).buffer
    const fetchBlob = vi.fn().mockResolvedValue(new Blob([png], { type: 'image/png' }))
    const user = userEvent.setup()
    render(
      <SupportTicketAttachmentList
        fetchBlob={fetchBlob}
        downloadFile={downloadFile}
        attachments={[
          {
            id: 'img1',
            fileName: 'a.png',
            contentType: 'image/png',
            downloadPath: '/api/t/att',
          },
        ]}
      />,
    )
    expect(screen.getByText('Carregando miniatura…')).toBeInTheDocument()
    await waitFor(() => {
      expect(fetchBlob).toHaveBeenCalledWith('/api/t/att')
    })
    const thumb = await screen.findByRole('button', { name: /Ampliar imagem: a\.png/ })
    expect(thumb.querySelector('img')).toBeTruthy()

    await user.click(thumb)
    const dialog = await screen.findByRole('dialog', { name: 'Visualizar anexo: a.png' })
    expect(dialog.querySelector('img.support-attachment-modal__img')).toBeTruthy()

    await user.click(screen.getByRole('button', { name: 'Fechar visualização' }))
    expect(screen.queryByRole('dialog', { name: 'Visualizar anexo: a.png' })).not.toBeInTheDocument()
  })

  it('closes image modal on Escape', async () => {
    const fetchBlob = vi.fn().mockResolvedValue(new Blob(['x'], { type: 'image/jpeg' }))
    const user = userEvent.setup()
    render(
      <SupportTicketAttachmentList
        fetchBlob={fetchBlob}
        downloadFile={vi.fn()}
        attachments={[
          {
            id: 'i1',
            fileName: 'x.jpg',
            contentType: 'image/jpeg',
            downloadPath: '/a',
          },
        ]}
      />,
    )
    await screen.findByRole('button', { name: /Ampliar imagem/ })
    await user.click(screen.getByRole('button', { name: /Ampliar imagem/ }))
    expect(await screen.findByRole('dialog')).toBeInTheDocument()
    await user.keyboard('{Escape}')
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })
  })
})
