import { useParams, useNavigate } from 'react-router-dom'
import { newsList } from '../data/news'
import { ArrowLeft } from 'lucide-react'

export function NewsDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const news = newsList.find((n) => n.id === parseInt(id || '0', 10))

  if (!news) {
    return (
      <div className="flex flex-col items-center justify-center flex-1 pb-20">
        <p className="text-gray-600">Notícia não encontrada.</p>
        <button
          onClick={() => navigate('/')}
          className="mt-4 text-ffc-green font-bold"
        >
          Voltar
        </button>
      </div>
    )
  }

  return (
    <div className="flex flex-col flex-1 pb-20">
      <div className="flex items-center gap-2 px-4 pt-2">
        <button
          onClick={() => navigate(-1)}
          className="p-1 -ml-1 text-ffc-green"
          aria-label="Voltar"
        >
          <ArrowLeft size={24} />
        </button>
        <h1 className="text-ffc-green text-xl font-bold uppercase flex-1 text-center -ml-8">
          NOTÍCIAS
        </h1>
      </div>
      <article className="flex flex-col">
        <div className="w-full aspect-[4/3] overflow-hidden">
          <img
            src={news.image}
            alt={news.title}
            className="w-full h-full object-cover"
          />
        </div>
        <div className="px-4 py-4">
          <h2 className="font-bold text-black text-lg uppercase">{news.title}</h2>
          <div className="mt-3 space-y-3 text-black text-sm leading-relaxed">
            {news.body.split('\n\n').map((paragraph, i) => (
              <p key={i}>{paragraph}</p>
            ))}
          </div>
        </div>
      </article>
    </div>
  )
}
