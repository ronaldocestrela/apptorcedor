import { profileData } from '../data/profile'
import { Badge } from '../components/Badge'

export function ProfilePage() {
  const { name, membershipType, memberSince, joinedDate, badges } = profileData

  return (
    <div className="flex flex-col flex-1 pb-20">
      <div className="px-4 py-4 flex flex-col gap-4">
        <section className="flex gap-4 items-start">
          <div className="flex-shrink-0 w-20 h-20 rounded-full bg-ffc-green" />
          <div className="flex-1 min-w-0">
            <h2 className="font-bold text-black uppercase">{name}</h2>
            <p className="text-black text-sm mt-1">{membershipType}</p>
            <ul className="mt-2 space-y-1 text-black text-sm">
              <li className="flex items-center gap-2">
                <span className="w-1.5 h-1.5 rounded-full bg-black" />
                {memberSince}
              </li>
              <li className="flex items-center gap-2">
                <span className="w-1.5 h-1.5 rounded-full bg-black" />
                {joinedDate}
              </li>
            </ul>
          </div>
        </section>
        <section>
          <h3 className="font-bold text-black uppercase text-sm mb-3">
            PRÊMIOS
          </h3>
          <div className="flex gap-3">
            {badges.map((award) => (
              <Badge key={award.id} {...award} />
            ))}
          </div>
        </section>
      </div>
    </div>
  )
}
