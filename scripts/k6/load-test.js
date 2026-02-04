import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter } from 'k6/metrics';

// Custom metrics
const voteRequests = new Counter('vote_requests');
const leaderboardRequests = new Counter('leaderboard_requests');

// Configuration
const BASE_URL = __ENV.BASE_URL || 'http://192.168.49.2:31798';
const GAMES = [
  'Apex Legends',
  'Fortnite',
  'Call of Duty',
  'Valorant',
  'Overwatch'
];

// 300:1 ratio - check leaderboard every 300 vote iterations
const VOTES_PER_LEADERBOARD = 300;

// Load test stages
export const options = {
  stages: [
    { duration: '2m', target: 500 },  // Ramp-up to 500 VUs over 2 minutes
    { duration: '5m', target: 500 },  // Sustain 500 VUs for 5 minutes
    { duration: '1m', target: 0 },    // Ramp-down to 0 VUs over 1 minute
  ],
  thresholds: {
    http_req_failed: ['rate<0.05'],        // HTTP errors should be less than 5%
    http_req_duration: ['p(95)<500'],       // 95% of requests should be below 500ms
    'http_req_duration{endpoint:votes}': ['p(95)<500'],
    'http_req_duration{endpoint:leaderboard}': ['p(95)<300'],
  },
};

// Global iteration counter (per VU)
let iterationCount = 0;

export default function () {
  iterationCount++;

  // Every 300 iterations, request leaderboard
  if (iterationCount % VOTES_PER_LEADERBOARD === 0) {
    getLeaderboard();
  } else {
    submitVote();
  }

  // Small think time to simulate realistic user behavior
  sleep(0.1 + Math.random() * 0.2); // Random sleep between 100-300ms
}

function submitVote() {
  // Generate random vote
  const game = GAMES[Math.floor(Math.random() * GAMES.length)];
  const userId = `user_${__VU}_${__ITER}`; // Unique user ID per VU and iteration

  const payload = JSON.stringify({
    game: game,
    id: userId,
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
    tags: { endpoint: 'votes' },
  };

  const response = http.post(`${BASE_URL}/votes`, payload, params);

  // Increment custom metric
  voteRequests.add(1);

  // Validate response
  check(response, {
    'vote: status is 200': (r) => r.status === 200,
    'vote: response contains confirmation': (r) => r.body.includes('recorded'),
  });
}

function getLeaderboard() {
  const params = {
    tags: { endpoint: 'leaderboard' },
  };

  const response = http.get(`${BASE_URL}/leaderboard`, params);

  // Increment custom metric
  leaderboardRequests.add(1);

  // Validate response
  check(response, {
    'leaderboard: status is 200': (r) => r.status === 200,
    'leaderboard: valid JSON': (r) => {
      try {
        const data = JSON.parse(r.body);
        return typeof data === 'object';
      } catch {
        return false;
      }
    },
    'leaderboard: contains games': (r) => {
      try {
        const data = JSON.parse(r.body);
        return Object.keys(data).length > 0;
      } catch {
        return false;
      }
    },
  });
}
