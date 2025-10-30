import { useState, useEffect } from 'react';
import type { Match, Player } from '../types';
import { fetchMatches, fetchPlayers, updateMatch, deleteMatch } from '../services/api';
import authService from '../services/authService';
import './Management.css';

export function Management() {
  const [matches, setMatches] = useState<Match[]>([]);
  const [players, setPlayers] = useState<Player[]>([]);
  const [selectedMatch, setSelectedMatch] = useState<Match | null>(null);
  const [editForm, setEditForm] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      const [matchesData, playersData] = await Promise.all([
        fetchMatches(),
        fetchPlayers()
      ]);
      setMatches(matchesData);
      setPlayers(playersData);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load data');
    } finally {
      setLoading(false);
    }
  };

  const handleEdit = (match: Match) => {
    setSelectedMatch(match);
    setEditForm({
      player1Id: match.player1Id,
      player2Id: match.player2Id,
      matchDate: new Date(match.matchDate).toISOString().split('T')[0],
      player1Score: match.player1Score,
      player2Score: match.player2Score,
      player1Average: match.player1Average,
      player2Average: match.player2Average,
      player1180s: match.player1180s,
      player2180s: match.player2180s,
      player1HighestCheckout: match.player1HighestCheckout,
      player2HighestCheckout: match.player2HighestCheckout,
      season: match.season,
      round: match.round
    });
    setError(null);
    setSuccess(null);
  };

  const handleSave = async () => {
    if (!selectedMatch || !editForm) return;

    try {
      const token = await authService.getAccessToken();
      if (!token) {
        setError('Not authenticated');
        return;
      }

      await updateMatch(selectedMatch.id, {
        ...editForm,
        matchDate: new Date(editForm.matchDate).toISOString()
      }, token);

      setSuccess('Match updated successfully!');
      setSelectedMatch(null);
      setEditForm(null);
      await loadData();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update match');
    }
  };

  const handleDelete = async (matchId: number) => {
    if (!confirm('Are you sure you want to delete this match?')) return;

    try {
      const token = await authService.getAccessToken();
      if (!token) {
        setError('Not authenticated');
        return;
      }

      await deleteMatch(matchId, token);
      setSuccess('Match deleted successfully!');
      await loadData();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete match');
    }
  };

  const handleCancel = () => {
    setSelectedMatch(null);
    setEditForm(null);
    setError(null);
    setSuccess(null);
  };

  if (loading) {
    return (
      <div className="management-container">
        <div className="loading">Loading management data...</div>
      </div>
    );
  }

  return (
    <div className="management-container">
      <h1>🔐 Match Management</h1>
      <p className="management-description">
        Manage match data, statistics, results and players. Only administrators can access this section.
      </p>

      {error && (
        <div className="alert alert-error">
          ❌ {error}
        </div>
      )}

      {success && (
        <div className="alert alert-success">
          ✅ {success}
        </div>
      )}

      {editForm && selectedMatch ? (
        <div className="edit-form-container">
          <h2>Edit Match #{selectedMatch.id}</h2>
          <form className="edit-form" onSubmit={(e) => { e.preventDefault(); handleSave(); }}>
            <div className="form-row">
              <div className="form-group">
                <label>Player 1:</label>
                <select 
                  value={editForm.player1Id} 
                  onChange={(e) => setEditForm({...editForm, player1Id: Number(e.target.value)})}
                  required
                >
                  {players.map(p => (
                    <option key={p.id} value={p.id}>{p.name}</option>
                  ))}
                </select>
              </div>

              <div className="form-group">
                <label>Player 2:</label>
                <select 
                  value={editForm.player2Id} 
                  onChange={(e) => setEditForm({...editForm, player2Id: Number(e.target.value)})}
                  required
                >
                  {players.map(p => (
                    <option key={p.id} value={p.id}>{p.name}</option>
                  ))}
                </select>
              </div>
            </div>

            <div className="form-row">
              <div className="form-group">
                <label>Match Date:</label>
                <input 
                  type="date" 
                  value={editForm.matchDate} 
                  onChange={(e) => setEditForm({...editForm, matchDate: e.target.value})}
                  required
                />
              </div>

              <div className="form-group">
                <label>Season:</label>
                <input 
                  type="text" 
                  value={editForm.season} 
                  onChange={(e) => setEditForm({...editForm, season: e.target.value})}
                  required
                />
              </div>

              <div className="form-group">
                <label>Round:</label>
                <input 
                  type="text" 
                  value={editForm.round} 
                  onChange={(e) => setEditForm({...editForm, round: e.target.value})}
                  required
                />
              </div>
            </div>

            <div className="form-row">
              <div className="form-group">
                <label>Player 1 Score:</label>
                <input 
                  type="number" 
                  value={editForm.player1Score} 
                  onChange={(e) => setEditForm({...editForm, player1Score: Number(e.target.value)})}
                  required
                  min="0"
                />
              </div>

              <div className="form-group">
                <label>Player 2 Score:</label>
                <input 
                  type="number" 
                  value={editForm.player2Score} 
                  onChange={(e) => setEditForm({...editForm, player2Score: Number(e.target.value)})}
                  required
                  min="0"
                />
              </div>
            </div>

            <div className="form-row">
              <div className="form-group">
                <label>Player 1 Average:</label>
                <input 
                  type="number" 
                  step="0.01"
                  value={editForm.player1Average} 
                  onChange={(e) => setEditForm({...editForm, player1Average: Number(e.target.value)})}
                  required
                  min="0"
                />
              </div>

              <div className="form-group">
                <label>Player 2 Average:</label>
                <input 
                  type="number" 
                  step="0.01"
                  value={editForm.player2Average} 
                  onChange={(e) => setEditForm({...editForm, player2Average: Number(e.target.value)})}
                  required
                  min="0"
                />
              </div>
            </div>

            <div className="form-row">
              <div className="form-group">
                <label>Player 1 180s:</label>
                <input 
                  type="number" 
                  value={editForm.player1180s} 
                  onChange={(e) => setEditForm({...editForm, player1180s: Number(e.target.value)})}
                  required
                  min="0"
                />
              </div>

              <div className="form-group">
                <label>Player 2 180s:</label>
                <input 
                  type="number" 
                  value={editForm.player2180s} 
                  onChange={(e) => setEditForm({...editForm, player2180s: Number(e.target.value)})}
                  required
                  min="0"
                />
              </div>
            </div>

            <div className="form-row">
              <div className="form-group">
                <label>Player 1 Highest Checkout:</label>
                <input 
                  type="number" 
                  value={editForm.player1HighestCheckout} 
                  onChange={(e) => setEditForm({...editForm, player1HighestCheckout: Number(e.target.value)})}
                  required
                  min="0"
                />
              </div>

              <div className="form-group">
                <label>Player 2 Highest Checkout:</label>
                <input 
                  type="number" 
                  value={editForm.player2HighestCheckout} 
                  onChange={(e) => setEditForm({...editForm, player2HighestCheckout: Number(e.target.value)})}
                  required
                  min="0"
                />
              </div>
            </div>

            <div className="form-actions">
              <button type="submit" className="btn btn-primary">💾 Save Changes</button>
              <button type="button" className="btn btn-secondary" onClick={handleCancel}>❌ Cancel</button>
            </div>
          </form>
        </div>
      ) : (
        <div className="matches-list">
          <h2>Matches</h2>
          <table className="management-table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Date</th>
                <th>Match</th>
                <th>Score</th>
                <th>Season</th>
                <th>Round</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {matches.map(match => {
                const player1 = players.find(p => p.id === match.player1Id);
                const player2 = players.find(p => p.id === match.player2Id);
                return (
                  <tr key={match.id}>
                    <td>{match.id}</td>
                    <td>{new Date(match.matchDate).toLocaleDateString()}</td>
                    <td>
                      {player1?.name || 'Unknown'} vs {player2?.name || 'Unknown'}
                    </td>
                    <td>{match.player1Score} - {match.player2Score}</td>
                    <td>{match.season}</td>
                    <td>{match.round}</td>
                    <td className="actions">
                      <button 
                        className="btn btn-edit" 
                        onClick={() => handleEdit(match)}
                        title="Edit match"
                      >
                        ✏️ Edit
                      </button>
                      <button 
                        className="btn btn-delete" 
                        onClick={() => handleDelete(match.id)}
                        title="Delete match"
                      >
                        🗑️ Delete
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
