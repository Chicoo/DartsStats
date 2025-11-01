import { useState, useEffect } from 'react';
import type { Player, Match } from './types';
import { fetchPlayers, fetchMatches, fetchRounds } from './services/api';
import { VenuePanel } from './components/VenuePanel';
import './App.css';

type SortColumn = 'name' | 'played' | 'won' | 'lost' | 'average' | '180s';
type SortDirection = 'asc' | 'desc';

interface PlayerStats {
  player: Player;
  played: number;
  won: number;
  lost: number;
  average: number;
  total180s: number;
}

function App() {
  const [players, setPlayers] = useState<Player[]>([]);
  const [matches, setMatches] = useState<Match[]>([]);
  const [rounds, setRounds] = useState<string[]>([]);
  const [selectedSeason, setSelectedSeason] = useState('2025');
  const [selectedRound, setSelectedRound] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [sortColumn, setSortColumn] = useState<SortColumn>('won');
  const [sortDirection, setSortDirection] = useState<SortDirection>('desc');
  const [showVenuePanel, setShowVenuePanel] = useState(false);
  const [isVenuePanelCollapsed, setIsVenuePanelCollapsed] = useState(false);

  // Update body class when venue panel state changes
  useEffect(() => {
    if (showVenuePanel && !isVenuePanelCollapsed) {
      document.body.classList.add('venue-panel-open');
    } else {
      document.body.classList.remove('venue-panel-open');
    }
  }, [showVenuePanel, isVenuePanelCollapsed]);

  useEffect(() => {
    const loadData = async () => {
      try {
        setLoading(true);
        const [playersData, matchesData, roundsData] = await Promise.all([
          fetchPlayers(),
          fetchMatches(selectedSeason, selectedRound || undefined),
          fetchRounds(selectedSeason)
        ]);
        setPlayers(playersData);
        setMatches(matchesData);
        setRounds(roundsData);
        setError(null);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'An error occurred');
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, [selectedSeason, selectedRound]);

  const handleSort = (column: SortColumn) => {
    if (sortColumn === column) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
    } else {
      setSortColumn(column);
      setSortDirection('desc');
    }
  };

  const calculatePlayerStats = (): PlayerStats[] => {
    return players.map(player => {
      const playerMatches = matches.filter(
        m => m.player1Id === player.id || m.player2Id === player.id
      );
      const wins = playerMatches.filter(m => 
        (m.player1Id === player.id && m.player1Score > m.player2Score) ||
        (m.player2Id === player.id && m.player2Score > m.player1Score)
      ).length;
      
      const totalAverage = playerMatches.reduce((acc, m) => {
        return acc + (m.player1Id === player.id ? m.player1Average : m.player2Average);
      }, 0);
      
      const total180s = playerMatches.reduce((acc, m) => {
        return acc + (m.player1Id === player.id ? m.player1180s : m.player2180s);
      }, 0);

      return {
        player,
        played: playerMatches.length,
        won: wins,
        lost: playerMatches.length - wins,
        average: playerMatches.length > 0 ? totalAverage / playerMatches.length : 0,
        total180s
      };
    });
  };

  const sortedPlayerStats = (): PlayerStats[] => {
    const stats = calculatePlayerStats();
    
    return stats.sort((a, b) => {
      let comparison = 0;
      
      switch (sortColumn) {
        case 'name':
          comparison = a.player.name.localeCompare(b.player.name);
          break;
        case 'played':
          comparison = a.played - b.played;
          break;
        case 'won':
          comparison = a.won - b.won;
          break;
        case 'lost':
          comparison = a.lost - b.lost;
          break;
        case 'average':
          comparison = a.average - b.average;
          break;
        case '180s':
          comparison = a.total180s - b.total180s;
          break;
      }
      
      return sortDirection === 'asc' ? comparison : -comparison;
    });
  };

  const getSortIcon = (column: SortColumn): string => {
    if (sortColumn !== column) return '⏸️';
    return sortDirection === 'asc' ? '⬆️' : '⬇️';
  };

  const handleRoundChange = (round: string) => {
    setSelectedRound(round);
    if (round && round !== '') {
      setShowVenuePanel(true);
    } else {
      setShowVenuePanel(false);
    }
  };

  if (loading) {
    return (
      <div className="container">
        <div className="loading">
          <div>🎯 Loading Premier League Darts data...</div>
        </div>
      </div>
    );
  }
  if (error) {
    return (
      <div className="container">
        <div className="error">
          <div>❌ Error: {error}</div>
        </div>
      </div>
    );
  }

  return (
    <div className={`container ${showVenuePanel && !isVenuePanelCollapsed ? 'with-venue-panel' : ''}`}>
      <h1>Premier League Darts {selectedSeason}</h1>
      
      <div className="season-selector">
        <label>
          Select Season:
          <select 
            value={selectedSeason} 
            onChange={(e) => setSelectedSeason(e.target.value)}
          >
            <option value="2025">2025</option>
            <option value="2024">2024</option>
          </select>
        </label>
        
        <label style={{ marginLeft: '1rem' }}>
          Select Round:
          <select 
            value={selectedRound} 
            onChange={(e) => handleRoundChange(e.target.value)}
          >
            <option value="">All Rounds</option>
            {rounds.map(round => (
              <option key={round} value={round}>{round}</option>
            ))}
          </select>
        </label>
      </div>

      <div className="standings">
        <h2>🏆 Player Standings</h2>
        {players.length === 0 ? (
          <div style={{textAlign: 'center', padding: '2rem', color: '#718096'}}>
            📊 No players data available
          </div>
        ) : (
          <table>
            <thead>
              <tr>
                <th onClick={() => handleSort('name')} style={{ cursor: 'pointer' }}>
                  � Player {getSortIcon('name')}
                </th>
                <th onClick={() => handleSort('played')} style={{ cursor: 'pointer' }}>
                  🎮 Played {getSortIcon('played')}
                </th>
                <th onClick={() => handleSort('won')} style={{ cursor: 'pointer' }}>
                  🏆 Won {getSortIcon('won')}
                </th>
                <th onClick={() => handleSort('lost')} style={{ cursor: 'pointer' }}>
                  ❌ Lost {getSortIcon('lost')}
                </th>
                <th onClick={() => handleSort('average')} style={{ cursor: 'pointer' }}>
                  📊 Average {getSortIcon('average')}
                </th>
                <th onClick={() => handleSort('180s')} style={{ cursor: 'pointer' }}>
                  🎯 180s {getSortIcon('180s')}
                </th>
              </tr>
            </thead>
          <tbody>
            {sortedPlayerStats().map(stats => (
              <tr key={stats.player.id}>
                <td>
                  <div className="player-name">
                    <strong>{stats.player.name}</strong>
                    <div className="player-nickname">"{stats.player.nickname}"</div>
                  </div>
                </td>
                <td>{stats.played}</td>
                <td>{stats.won}</td>
                <td>{stats.lost}</td>
                <td>{stats.average.toFixed(2)}</td>
                <td>{stats.total180s}</td>
              </tr>
            ))}
          </tbody>
        </table>
        )}
      </div>

      <div className="recent-matches">
        <h2>🔥 Recent Matches</h2>
        <div className="matches-grid">
          {matches.slice(0, 12).map(match => (
            <div key={match.id} className="match-card">
              <div className="match-date">
                📅 {new Date(match.matchDate).toLocaleDateString('en-US', { 
                  weekday: 'short', 
                  year: 'numeric', 
                  month: 'short', 
                  day: 'numeric' 
                })}
                <div className="match-round">🏁 {match.round}</div>
              </div>
              <div className="match-players">
                <div className="player">
                  <div className="player-info">
                    <span className="player-name">🎯 {players.find(p => p.id === match.player1Id)?.name || 'Unknown'}</span>
                    <span className="player-nickname">"{players.find(p => p.id === match.player1Id)?.nickname || ''}"</span>
                  </div>
                  <span className="score">{match.player1Score}</span>
                </div>
                <div className="player">
                  <div className="player-info">
                    <span className="player-name">🎯 {players.find(p => p.id === match.player2Id)?.name || 'Unknown'}</span>
                    <span className="player-nickname">"{players.find(p => p.id === match.player2Id)?.nickname || ''}"</span>
                  </div>
                  <span className="score">{match.player2Score}</span>
                </div>
              </div>
              <div className="match-stats">
                <div>
                  <span>📊 Averages:</span> 
                  <span>{match.player1Average.toFixed(2)} - {match.player2Average.toFixed(2)}</span>
                </div>
                <div>
                  <span>🎯 180s:</span>
                  <span>{match.player1180s} - {match.player2180s}</span>
                </div>
                <div>
                  <span>🎪 Highest Checkouts:</span>
                  <span>{match.player1HighestCheckout} - {match.player2HighestCheckout}</span>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      <VenuePanel 
        selectedRound={selectedRound}
        isVisible={showVenuePanel}
        onCollapseChange={setIsVenuePanelCollapsed}
      />
    </div>
  );
}

export default App;
