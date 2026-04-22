import { useCallback, useEffect, useId, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { isSupportAttachmentImageContentType } from './supportTicketAttachmentContent'
import './supportTicketAttachments.css'

export type SupportTicketAttachmentListItem = {
  id: string
  fileName: string
  contentType: string
  downloadPath: string
}

type SupportTicketAttachmentListProps = {
  attachments: SupportTicketAttachmentListItem[]
  fetchBlob: (downloadPath: string) => Promise<Blob>
  downloadFile: (downloadPath: string, fileName: string) => Promise<void>
  /** Extra class on `<ul>`, e.g. `support-attachment-list--light` for admin */
  listClassName?: string
}

function ImageRow(props: {
  fileName: string
  contentType: string
  downloadPath: string
  fetchBlob: (path: string) => Promise<Blob>
  onDownload: () => void
}) {
  const { fileName, contentType, downloadPath, fetchBlob, onDownload } = props
  const [url, setUrl] = useState<string | null>(null)
  const [loadError, setLoadError] = useState(false)
  const [open, setOpen] = useState(false)
  const closeBtnRef = useRef<HTMLButtonElement | null>(null)
  const captionId = useId()

  useEffect(() => {
    let cancelled = false
    let objectUrl: string | null = null
    void (async () => {
      try {
        const blob = await fetchBlob(downloadPath)
        if (cancelled)
          return
        objectUrl = URL.createObjectURL(blob)
        setUrl(objectUrl)
        setLoadError(false)
      }
      catch {
        if (!cancelled)
          setLoadError(true)
      }
    })()
    return () => {
      cancelled = true
      if (objectUrl)
        URL.revokeObjectURL(objectUrl)
    }
  }, [downloadPath, fetchBlob])

  const close = useCallback(() => setOpen(false), [])

  useEffect(() => {
    if (!open)
      return
    const t = setTimeout(() => closeBtnRef.current?.focus(), 0)
    function onKey(e: KeyboardEvent) {
      if (e.key === 'Escape') {
        e.preventDefault()
        close()
      }
    }
    document.addEventListener('keydown', onKey)
    return () => {
      clearTimeout(t)
      document.removeEventListener('keydown', onKey)
    }
  }, [open, close])

  const thumbLabel = `Ampliar imagem: ${fileName}`

  if (loadError) {
    return (
      <li className="support-attachment-list__row">
        <span className="support-attachment__meta">{fileName}</span>
        <button
          type="button"
          className="support-attachment__link"
          onClick={() => void onDownload()}
        >
          Baixar
        </button>
        <span className="support-attachment__meta">({contentType})</span>
      </li>
    )
  }

  if (!url) {
    return (
      <li className="support-attachment-list__row">
        <span className="support-attachment__loading" aria-hidden>Carregando miniatura…</span>
        <span className="sr-only">{`Carregando anexo: ${fileName}`}</span>
      </li>
    )
  }

  const modal = open
    ? createPortal(
        <div
          className="support-attachment-modal"
          role="presentation"
          onClick={e => (e.target === e.currentTarget ? close() : null)}
        >
          <div
            className="support-attachment-modal__dialog"
            role="dialog"
            aria-modal="true"
            aria-label={`Visualizar anexo: ${fileName}`}
            aria-describedby={captionId}
            onClick={e => e.stopPropagation()}
          >
            <button
              ref={closeBtnRef}
              type="button"
              className="support-attachment-modal__close"
              aria-label="Fechar visualização"
              onClick={close}
            >
              ×
            </button>
            <div className="support-attachment-modal__img-wrap">
              <img
                className="support-attachment-modal__img"
                src={url}
                alt={fileName}
              />
            </div>
            <p id={captionId} className="support-attachment-modal__caption">{fileName}</p>
          </div>
        </div>,
        document.body,
      )
    : null

  return (
    <li className="support-attachment-list__row">
      <button
        type="button"
        className="support-attachment__thumb"
        onClick={() => setOpen(true)}
        aria-label={thumbLabel}
      >
        <img src={url} alt="" width={88} height={88} />
      </button>
      <div className="support-attachment__meta" aria-hidden>
        {fileName}
        {' '}
        (
        {contentType}
        )
      </div>
      <button
        type="button"
        className="support-attachment__download-btn"
        onClick={() => void onDownload()}
      >
        Baixar
      </button>
      {modal}
    </li>
  )
}

function NonImageRow(props: {
  fileName: string
  contentType: string
  onDownload: () => void
}) {
  const { fileName, contentType, onDownload } = props
  return (
    <li className="support-attachment-list__row">
      <button
        type="button"
        className="support-attachment__link"
        onClick={() => void onDownload()}
      >
        {fileName}
      </button>
      <span className="support-attachment__meta" aria-hidden>
        (
        {contentType}
        )
      </span>
    </li>
  )
}

export function SupportTicketAttachmentList(props: SupportTicketAttachmentListProps) {
  const { attachments, fetchBlob, downloadFile, listClassName = '' } = props
  if (attachments.length === 0)
    return null

  const ulClass = `support-attachment-list ${listClassName}`.trim()

  return (
    <ul className={ulClass}>
        {attachments.map((a) => {
          if (isSupportAttachmentImageContentType(a.contentType)) {
            return (
              <ImageRow
                key={a.id}
                fileName={a.fileName}
                contentType={a.contentType}
                downloadPath={a.downloadPath}
                fetchBlob={fetchBlob}
                onDownload={() => void downloadFile(a.downloadPath, a.fileName)}
              />
            )
          }
          return (
            <NonImageRow
              key={a.id}
              fileName={a.fileName}
              contentType={a.contentType}
              onDownload={() => void downloadFile(a.downloadPath, a.fileName)}
            />
          )
        })}
    </ul>
  )
}
