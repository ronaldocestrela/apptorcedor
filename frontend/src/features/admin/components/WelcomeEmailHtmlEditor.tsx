import Placeholder from '@tiptap/extension-placeholder'
import { EditorContent, useEditor } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import { useEffect, type ReactNode } from 'react'
import './WelcomeEmailHtmlEditor.css'

type WelcomeEmailHtmlEditorProps = {
  /** HTML inicial (ex.: vindo do banco). Atualize `key` no pai após reload para remontar. */
  initialHtml: string
  editable: boolean
  onChange: (html: string) => void
}

function ToolbarButton({
  onClick,
  disabled,
  pressed,
  title,
  children,
}: {
  onClick: () => void
  disabled?: boolean
  pressed?: boolean
  title: string
  children: ReactNode
}) {
  return (
    <button
      type="button"
      title={title}
      aria-label={title}
      aria-pressed={pressed ?? false}
      disabled={disabled}
      onMouseDown={(e) => e.preventDefault()}
      onClick={() => onClick()}
    >
      {children}
    </button>
  )
}

export function WelcomeEmailHtmlEditor({ initialHtml, editable, onChange }: WelcomeEmailHtmlEditorProps) {
  const editor = useEditor(
    {
      extensions: [
        StarterKit.configure({
          heading: { levels: [2, 3] },
          link: {
            openOnClick: false,
            autolink: true,
            defaultProtocol: 'https',
          },
        }),
        Placeholder.configure({
          placeholder:
            'Digite o texto. Use a barra para negrito, itálico, listas, etc. Inclua {{BannerImage}} e {{Name}} como texto se precisar.',
        }),
      ],
      content: initialHtml || '',
      editable,
      editorProps: {
        attributes: {
          class: 'welcome-email-prose',
          spellCheck: 'true',
          role: 'textbox',
          'aria-multiline': 'true',
          'aria-label': 'Corpo do e-mail em HTML',
        },
      },
      onUpdate: ({ editor: ed }) => {
        onChange(ed.getHTML())
      },
    },
    [],
  )

  useEffect(() => {
    if (!editor)
      return
    editor.setEditable(editable)
  }, [editable, editor])

  if (!editor)
    return <div className="welcome-email-editor welcome-email-editor__loading">Carregando editor…</div>

  const insertLink = () => {
    const prev = editor.getAttributes('link').href as string | undefined
    const url = window.prompt('URL do link (https://…)', prev || 'https://')
    if (url === null)
      return
    const t = url.trim()
    if (t === '') {
      editor.chain().focus().extendMarkRange('link').unsetLink().run()
      return
    }
    editor.chain().focus().extendMarkRange('link').setLink({ href: t }).run()
  }

  return (
    <div className="welcome-email-editor">
      {editable ? (
        <div className="welcome-email-editor__toolbar" role="toolbar" aria-label="Formatação do e-mail">
          <ToolbarButton
            title="Negrito"
            pressed={editor.isActive('bold')}
            onClick={() => editor.chain().focus().toggleBold().run()}
          >
            <strong>B</strong>
          </ToolbarButton>
          <ToolbarButton
            title="Itálico"
            pressed={editor.isActive('italic')}
            onClick={() => editor.chain().focus().toggleItalic().run()}
          >
            <em>I</em>
          </ToolbarButton>
          <ToolbarButton
            title="Sublinhado"
            pressed={editor.isActive('underline')}
            onClick={() => editor.chain().focus().toggleUnderline().run()}
          >
            <span style={{ textDecoration: 'underline' }}>U</span>
          </ToolbarButton>
          <ToolbarButton
            title="Riscado"
            pressed={editor.isActive('strike')}
            onClick={() => editor.chain().focus().toggleStrike().run()}
          >
            <s>S</s>
          </ToolbarButton>
          <span className="welcome-email-editor__sep" aria-hidden />
          <ToolbarButton
            title="Título 2"
            pressed={editor.isActive('heading', { level: 2 })}
            onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
          >
            H2
          </ToolbarButton>
          <ToolbarButton
            title="Título 3"
            pressed={editor.isActive('heading', { level: 3 })}
            onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()}
          >
            H3
          </ToolbarButton>
          <span className="welcome-email-editor__sep" aria-hidden />
          <ToolbarButton
            title="Lista com marcadores"
            pressed={editor.isActive('bulletList')}
            onClick={() => editor.chain().focus().toggleBulletList().run()}
          >
            • Lista
          </ToolbarButton>
          <ToolbarButton
            title="Lista numerada"
            pressed={editor.isActive('orderedList')}
            onClick={() => editor.chain().focus().toggleOrderedList().run()}
          >
            1. Lista
          </ToolbarButton>
          <ToolbarButton
            title="Citação"
            pressed={editor.isActive('blockquote')}
            onClick={() => editor.chain().focus().toggleBlockquote().run()}
          >
            “ ”
          </ToolbarButton>
          <ToolbarButton
            title="Linha horizontal"
            onClick={() => editor.chain().focus().setHorizontalRule().run()}
          >
            —
          </ToolbarButton>
          <ToolbarButton
            title="Código (trecho)"
            pressed={editor.isActive('code')}
            onClick={() => editor.chain().focus().toggleCode().run()}
          >
            {'<>'}
          </ToolbarButton>
          <ToolbarButton
            title="Link"
            pressed={editor.isActive('link')}
            onClick={() => insertLink()}
          >
            Link
          </ToolbarButton>
          <span className="welcome-email-editor__sep" aria-hidden />
          <ToolbarButton
            title="Desfazer"
            onClick={() => editor.chain().focus().undo().run()}
            disabled={!editor.can().undo()}
          >
            ↶
          </ToolbarButton>
          <ToolbarButton
            title="Refazer"
            onClick={() => editor.chain().focus().redo().run()}
            disabled={!editor.can().redo()}
          >
            ↷
          </ToolbarButton>
        </div>
      ) : null}
      <div className="welcome-email-editor__body">
        <EditorContent editor={editor} />
      </div>
      {editable ? (
        <p className="welcome-email-editor__hint">
          Atalhos: Ctrl+B negrito, Ctrl+I itálico, Ctrl+U sublinhado. O HTML gerado é salvo no banco; placeholders
          {' '}
          <code>{'{{Name}}'}</code>
          {' '}
          e
          {' '}
          <code>{'{{BannerImage}}'}</code>
          {' '}
          podem ser digitados no texto.
        </p>
      ) : null}
    </div>
  )
}
