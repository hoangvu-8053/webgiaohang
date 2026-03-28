import { useState, useEffect, useRef } from 'react';
import { chatApi } from '../api/endpoints';
import { API_BASE } from '../api/client';
import { useAuth } from '../context/AuthContext';
import useSignalR from '../hooks/useSignalR';

export default function ChatPage() {
  const { user } = useAuth();
  const [conversations, setConversations] = useState([]);
  const [activeUser, setActiveUser] = useState(null);
  const [messages, setMessages] = useState([]);
  const [inputValue, setInputValue] = useState('');
  const [loading, setLoading] = useState(true);
  const messagesEndRef = useRef(null);

  // Connect to ChatHub
  const { connection, isConnected } = useSignalR(`${API_BASE}/chatHub`);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    fetchConversations();
  }, []);

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  useEffect(() => {
    if (activeUser) {
      fetchMessages(activeUser);
      
      // Join conversation group via hub
      if (connection && isConnected) {
        const conversationId = getConversationId(user.username, activeUser);
        connection.invoke('JoinConversation', conversationId)
          .catch(err => console.error('JoinConversation error:', err));
        
        // Listen for new messages
        connection.on('ReceiveMessage', (message) => {
          setMessages(prev => {
            if (prev.find(m => m.id === message.id)) return prev;
            return [...prev, message];
          });
        });

        return () => {
          connection.off('ReceiveMessage');
          connection.invoke('LeaveConversation', conversationId)
            .catch(err => console.error('LeaveConversation error:', err));
        };
      }
    }
  }, [activeUser, connection, isConnected, user.username]);

  const getConversationId = (u1, u2) => {
    return [u1, u2].sort().join('_');
  };

  const fetchConversations = async () => {
    setLoading(true);
    try {
      const res = await chatApi.getConversations();
      if (res.data.success) setConversations(res.data.conversations);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const fetchMessages = async (other) => {
    try {
      const res = await chatApi.getMessages(other);
      if (res.data.success) setMessages(res.data.messages);
    } catch (err) {
      console.error(err);
    }
  };

  const handleSend = async (e) => {
    e.preventDefault();
    if (!inputValue.trim() || !activeUser) return;
    
    const messageData = { 
      receiverUsername: activeUser, 
      content: inputValue,
      conversationId: getConversationId(user.username, activeUser)
    };

    try {
      // Send via API (for persistence)
      await chatApi.sendMessage(messageData);
      
      // Clear input
      setInputValue('');
      
      // If SignalR is connected, notify others (the Hub also handles this, but we can optimistically update)
      // Actually the Hub in ChatHub.cs invokes "ReceiveMessage" in the group, 
      // so if we are in that group, we will receive it back.
      
    } catch (err) {
      alert('Lỗi khi gửi tin nhắn.');
    }
  };

  return (
    <div className="animate-fade" style={{ display: 'grid', gridTemplateColumns: 'minmax(250px, 350px) 1fr', height: 'calc(100vh - 180px)', gap: '1.5rem' }}>
      <div className="card" style={{ padding: '1.5rem', display: 'flex', flexDirection: 'column' }}>
        <h2 style={{ fontSize: '1.5rem', fontWeight: 900, marginBottom: '2rem' }}>💬 Tin nhắn <span style={{ fontSize: '0.8rem', color: isConnected ? '#10b981' : '#ef4444' }}>● {isConnected ? 'Online' : 'Offline'}</span></h2>
        
        <div style={{ flex: 1, overflowY: 'auto' }}>
          {loading ? (
            <div style={{ textAlign: 'center', padding: '2rem' }}>Đang tải...</div>
          ) : conversations.length === 0 ? (
            <div style={{ textAlign: 'center', padding: '2rem', color: '#64748b' }}>Chưa có cuộc hội thoại nào.</div>
          ) : (
            conversations.map(c => (
              <div key={c.username} className="card" 
                   style={{ 
                     padding: '1rem', 
                     marginBottom: '0.75rem', 
                     cursor: 'pointer', 
                     border: activeUser === c.username ? '2px solid var(--primary)' : '1px solid var(--border)',
                     background: activeUser === c.username ? '#f1f5ff' : 'white',
                     transition: 'all 0.2s'
                   }}
                   onClick={() => setActiveUser(c.username)}>
                <div style={{ fontWeight: 800 }}>@{c.username}</div>
                <div style={{ fontSize: '0.8rem', color: '#94a3b8', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                  {c.lastMessage?.content || 'Bắt đầu trò chuyện ngay!'}
                </div>
              </div>
            ))
          )}
        </div>
      </div>

      <div className="card" style={{ padding: '0', display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
        {!activeUser ? (
          <div style={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', color: '#64748b' }}>
            <div style={{ fontSize: '5rem', marginBottom: '1.5rem' }}>🤖</div>
            <p style={{ fontSize: '1.25rem', fontWeight: 700 }}>Chọn một cuộc hội thoại để bắt đầu</p>
          </div>
        ) : (
          <>
            <div style={{ padding: '1.5rem', borderBottom: '1px solid #eef2ff', background: '#f8fbff', fontWeight: 900, fontSize: '1.25rem', display: 'flex', justifyContent: 'space-between' }}>
              <span>@ {activeUser}</span>
              <span style={{ fontSize: '0.75rem', fontWeight: 600, color: '#64748b' }}>Trạng thái: Ready ⚡</span>
            </div>
            
            <div style={{ flex: 1, padding: '1.5rem', overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              {messages.length === 0 && <div style={{ textAlign: 'center', color: '#94a3b8', margin: '2rem 0' }}>Bắt đầu cuộc trò chuyện với @{activeUser}</div>}
              {messages.map((m, idx) => {
                const isMine = m.senderUsername === user.username;
                return (
                  <div key={m.id || idx} style={{ display: 'flex', justifyContent: isMine ? 'flex-end' : 'flex-start' }}>
                    <div style={{ 
                      maxWidth: '70%', 
                      padding: '1rem', 
                      borderRadius: '16px', 
                      background: isMine ? 'linear-gradient(135deg, #6366f1 0%, #4f46e5 100%)' : '#f1f5f9',
                      color: isMine ? 'white' : 'var(--text)',
                      borderRadius: isMine ? '20px 20px 4px 20px' : '20px 20px 20px 4px',
                      boxShadow: '0 4px 10px rgba(0,0,0,0.05)',
                      transform: 'scale(1)',
                      transition: 'transform 0.1s active'
                    }}>
                      <div style={{ fontSize: '0.95rem', fontWeight: 600 }}>{m.content}</div>
                      <div style={{ fontSize: '0.65rem', marginTop: '0.25rem', opacity: 0.7, textAlign: 'right' }}>
                        {new Date(m.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                      </div>
                    </div>
                  </div>
                );
              })}
              <div ref={messagesEndRef} />
            </div>

            <form onSubmit={handleSend} style={{ padding: '1.5rem', background: 'white', borderTop: '1px solid #eef2ff', display: 'flex', gap: '1rem' }}>
              <input value={inputValue} onChange={(e) => setInputValue(e.target.value)} 
                     className="input" placeholder="Nhập tin nhắn..." 
                     style={{ borderRadius: '999px', background: '#f8fbff', flex: 1, border: '2px solid transparent' }} />
              <button className="btn btn-primary" type="submit" disabled={!isConnected} style={{ padding: '1rem 2rem' }}>
                GỬI 🚀
              </button>
            </form>
          </>
        )}
      </div>
    </div>
  );
}

