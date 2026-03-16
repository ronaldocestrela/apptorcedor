export interface Badge {
  id: string;
  name: string;
  icon: string;
  color: string;
}

export interface Profile {
  name: string;
  membershipType: string;
  memberSince: string;
  joinedDate: string;
  avatar?: string;
  badges: Badge[];
}

export const profileData: Profile = {
  name: 'FULANO DA SILVA',
  membershipType: 'SÓCIO TORCEDOR',
  memberSince: 'SÓCIO HÁ 3 ANOS',
  joinedDate: 'INGRESSOU 16/03/2023',
  badges: [
    { id: '1', name: 'Infinity', icon: 'infinity', color: 'bg-purple-500' },
    { id: '2', name: 'Diamond', icon: 'gem', color: 'bg-gray-500' },
    { id: '3', name: 'Crown', icon: 'crown', color: 'bg-gray-600' },
  ],
};
