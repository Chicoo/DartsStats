import { useState, useEffect } from 'react';
import type { VenueInfo } from '../types';
import { fetchVenueInfo } from '../services/api';
import './VenuePanel.css';

interface VenuePanelProps {
  readonly selectedRound: string;
  readonly isVisible: boolean;
  readonly onCollapseChange?: (isCollapsed: boolean) => void;
}

export function VenuePanel({ selectedRound, isVisible, onCollapseChange }: VenuePanelProps) {
  const [venueInfo, setVenueInfo] = useState<VenueInfo | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isCollapsed, setIsCollapsed] = useState(false);

  // Notify parent when collapse state changes
  const handleCollapseToggle = () => {
    const newCollapsedState = !isCollapsed;
    setIsCollapsed(newCollapsedState);
    onCollapseChange?.(newCollapsedState);
  };

  useEffect(() => {
    if (!selectedRound || !isVisible) {
      setVenueInfo(null);
      return;
    }

    // Reset to expanded state when panel becomes visible
    setIsCollapsed(false);
    onCollapseChange?.(false);

    const loadVenueInfo = async () => {
      setLoading(true);
      setError(null);
      
      try {
        const info = await fetchVenueInfo(selectedRound);
        setVenueInfo(info);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load venue information');
        setVenueInfo(null);
      } finally {
        setLoading(false);
      }
    };

    loadVenueInfo();
  }, [selectedRound, isVisible]);

  if (!isVisible) {
    return null;
  }

  return (
    <>
      {isCollapsed && (
        <button 
          className="floating-expand-button" 
          onClick={handleCollapseToggle} 
          aria-label="Expand venue panel"
        >
          ⏪
        </button>
      )}
      
      <div className={`venue-panel ${isCollapsed ? 'collapsed' : 'expanded'}`}>
        <div className="venue-panel-header">
          <h3>🏟️ Venue Information</h3>
          {!isCollapsed && (
            <button 
              className="collapse-button" 
              onClick={handleCollapseToggle} 
              aria-label="Collapse venue panel"
            >
              ⏩
            </button>
          )}
        </div>
      
      <div className={`venue-panel-content ${isCollapsed ? 'collapsed' : 'expanded'}`}>
        {loading && (
          <div className="venue-loading">
            <div className="loading-spinner">🎯</div>
            <p>Loading venue information...</p>
          </div>
        )}
        
        {error && (
          <div className="venue-error">
            <p>⚠️ {error}</p>
            <p>Unable to fetch venue details for {selectedRound}</p>
          </div>
        )}
        
        {venueInfo && !loading && (
          <div className="venue-details">
            {venueInfo.weather && (
              <div className="weather-info">
                <h4>🌤️ Event Day Weather</h4>
                <div className="weather-details">
                  <div className="weather-main">
                    <span className="weather-temp">{Math.round(venueInfo.weather.temperature || 0)}°C</span>
                    <span className="weather-desc">{venueInfo.weather.description}</span>
                  </div>
                  <div className="weather-stats">
                    {venueInfo.weather.humidity && (
                      <span className="weather-stat">💧 {venueInfo.weather.humidity}%</span>
                    )}
                    {venueInfo.weather.windSpeed && (
                      <span className="weather-stat">🌬️ {venueInfo.weather.windSpeed} m/s</span>
                    )}
                  </div>
                  <div className="event-date">
                    📅 {new Date(venueInfo.weather.eventDate).toLocaleDateString('en-GB', {
                      weekday: 'long',
                      year: 'numeric',
                      month: 'long',
                      day: 'numeric'
                    })}
                  </div>
                </div>
              </div>
            )}
            
            <div className="venue-main-info">
              <h4 className="venue-name">{venueInfo.name}</h4>
              <p className="venue-city">📍 {venueInfo.city}</p>
              
              {venueInfo.capacity && (
                <p className="venue-capacity">
                  👥 Capacity: {venueInfo.capacity}
                </p>
              )}
              
              {venueInfo.opened && (
                <p className="venue-opened">
                  🏗️ Opened: {venueInfo.opened}
                </p>
              )}
            </div>
            
            {venueInfo.image && (
              <div className="venue-image">
                <img 
                  src={venueInfo.image} 
                  alt={`${venueInfo.name} venue`}
                  onError={(e) => {
                    const target = e.target as HTMLImageElement;
                    target.style.display = 'none';
                  }}
                />
              </div>
            )}
            
            <div className="venue-description">
              <h5>About the Venue</h5>
              <p>{venueInfo.description}</p>
            </div>
            
            {venueInfo.website && (
              <div className="venue-links">
                <a 
                  href={venueInfo.website} 
                  target="_blank" 
                  rel="noopener noreferrer"
                  className="venue-link"
                >
                  🔗 Learn More
                </a>
              </div>
            )}
          </div>
        )}
        
        {!venueInfo && !loading && !error && selectedRound && (
          <div className="venue-no-info">
            <p>No venue information available for {selectedRound}</p>
          </div>
        )}
        </div>
      </div>
    </>
  );
}
