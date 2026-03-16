import type { NewsItem } from '../data/news'

interface NewsCardProps {
  news: NewsItem
}

export function NewsCard({ news }: NewsCardProps) {
  return (
    <article className="flex gap-3 bg-white rounded-lg shadow-sm overflow-hidden transition-shadow hover:shadow-md">
      <div className="flex-shrink-0 w-20 h-20">
        <img
          src={news.image}
          alt={news.title}
          className="w-full h-full object-cover"
        />
      </div>
      <div className="flex-1 min-w-0 py-1">
        <h2 className="font-bold text-black uppercase text-sm leading-tight">
          {news.title}
        </h2>
        <p className="text-black/80 text-xs mt-1 line-clamp-2">{news.summary}</p>
      </div>
    </article>
  )
}
