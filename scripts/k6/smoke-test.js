import http from 'k6/http';
import { check, sleep } from 'k6';

// Configuration
const BASE_URL = __ENV.BASE_URL || 'http://192.168.49.2:31798';
const GAMES = [
  'Apex Legends',
  'Fortnite',
  'Call of Duty',
  'Valorant',
  'Overwatch'
];

// Smoke test configuration - lightweight validation
export const options = {
  vus: 10,              // 10 virtual users
  duration: '30s',      // Run for 30 seconds
  thresholds: {
    http_req_failed: ['rate<0.01'],     // Less than 1% errors
    http_req_duration: ['p(95)<1000'],   // 95% requests under 1s
  },
};

export default function () {
  // Test vote submission
  const game = GAMES[Math.floor(Math.random() * GAMES.length)];
  const userId = `smoke_user_${__VU}_${__ITER}`;

  const votePayload = JSON.stringify({
    game: game,
    id: userId,
  });

  const voteResponse = http.post(`${BASE_URL}/votes`, votePayload, {
    headers: { 'Content-Type': 'application/json' },
  });

  check(voteResponse, {
    'smoke: vote status is 200': (r) => r.status === 200,
    'smoke: vote response valid': (r) => r.body.includes('recorded'),
  });

  sleep(1);

  // Test leaderboard endpoint
  const leaderboardResponse = http.get(`${BASE_URL}/leaderboard`);

  check(leaderboardResponse, {
    'smoke: leaderboard status is 200': (r) => r.status === 200,
    'smoke: leaderboard is valid JSON': (r) => {
      try {
        JSON.parse(r.body);
        return true;
      } catch {
        return false;
      }
    },
  });

  sleep(1);
}
