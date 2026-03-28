import { useEffect, useRef, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';

export default function useSignalR(hubUrl, options = {}) {
  const [connection, setConnection] = useState(null);
  const [isConnected, setIsConnected] = useState(false);
  const [error, setError] = useState(null);
  const reconnectTimerRef = useRef(null);

  const startConnection = useCallback(async (conn) => {
    if (!conn) return;
    try {
      if (conn.state === signalR.HubConnectionState.Disconnected) {
        await conn.start();
        console.log(`[SignalR] Connected to ${hubUrl}`);
        setIsConnected(true);
        setError(null);
      }
    } catch (err) {
      console.error(`[SignalR] Connection failed to ${hubUrl}:`, err);
      setError(err);
      setIsConnected(false);
      // Try again in 5 seconds
      reconnectTimerRef.current = setTimeout(() => startConnection(conn), 5000);
    }
  }, [hubUrl]);

  useEffect(() => {
    const token = localStorage.getItem('token');
    
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => token || '',
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    newConnection.onclose((err) => {
      setIsConnected(false);
      if (err) setError(err);
    });

    newConnection.onreconnecting((err) => {
      setIsConnected(false);
      console.warn('[SignalR] Reconnecting...', err);
    });

    newConnection.onreconnected(() => {
      setIsConnected(true);
      console.log('[SignalR] Reconnected.');
    });

    setConnection(newConnection);
    startConnection(newConnection);

    return () => {
      if (reconnectTimerRef.current) clearTimeout(reconnectTimerRef.current);
      if (newConnection) {
        newConnection.stop();
      }
    };
  }, [hubUrl, startConnection]);

  return { connection, isConnected, error };
}
