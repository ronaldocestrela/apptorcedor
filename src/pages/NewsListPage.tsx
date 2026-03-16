import { Link } from 'react-router-dom'
import { NewsCard } from '../components/NewsCard'
import { newsList } from '../data/news'

export function NewsListPage() {
  return (
    <div className="flex flex-col flex-1">
      <div className="flex flex-col gap-4 px-4 py-4">
        {newsList.map((news) => (
          <Link key={news.id} to={`/noticias/${news.id}`}>
            <NewsCard news={news} />
          </Link>
        ))}
      </div>
    </div>
  )
}
