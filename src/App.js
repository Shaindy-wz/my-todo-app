import React, { useState, useEffect } from 'react';
import service from './service';
import './App.css';

function App() {
  // --- States לניהול משתמשים ---
  const [isLoggedIn, setIsLoggedIn] = useState(!!localStorage.getItem('token'));
  const [isRegistering, setIsRegistering] = useState(false);
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [searchTerm, setSearchTerm] = useState('');

  // --- States לניהול משימות ---
  const [tasks, setTasks] = useState([]);
  const [newTaskName, setNewTaskName] = useState('');
  const [newTaskDate, setNewTaskDate] = useState('');

  // טעינת משימות מהשרת
  const loadTasks = async () => {
    try {
      const data = await service.getTasks();
      setTasks(data);
    } catch (error) {
      console.error("טעינה נכשלה:", error);
    }
  };

  useEffect(() => {
    if (isLoggedIn) {
      loadTasks();
    }
  }, [isLoggedIn]);

  // --- לוגיקת סטטיסטיקה וצבעים ---
  const completedCount = tasks.filter(t => t.isComplete).length;
  const totalCount = tasks.length;

  const getDateStyle = (dateStr) => {
    if (!dateStr) return { color: '#64748b' };
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const taskDate = new Date(dateStr);
    taskDate.setHours(0, 0, 0, 0);

    if (taskDate < today) return { color: '#94a3b8', textDecoration: 'line-through' }; // עבר - אפור
    if (taskDate.getTime() === today.getTime()) return { color: '#ef4444', fontWeight: 'bold' }; // היום - אדום
    return { color: '#10b981', fontWeight: 'bold' }; // עתיד - ירוק
  };

  // --- פונקציות אימות ---
  const handleLogin = async () => {
    try {
      if (!username || !password) return alert("נא להזין שם משתמש וסיסמה");
      await service.login(username, password);
      setIsLoggedIn(true);
    } catch (err) {
      alert("שם משתמש או סיסמה שגויים");
    }
  };

const handleRegister = async () => {
  try {
    if (!username || !password) return alert("נא להזין פרטים להרשמה");
    
    // שימוש ב-service במקום ב-fetch ידני
    await service.register(username, password);
    
    alert("נרשמת בהצלחה! עכשיו אפשר להתחבר");
    setIsRegistering(false);
  } catch (err) {
    alert("ההרשמה נכשלה. ייתכן ששם המשתמש תפוס");
  }
};

  const handleLogout = () => {
    service.logout();
    setIsLoggedIn(false);
    setTasks([]);
  };

  // --- פונקציות ניהול משימות ---
  const createTodo = async () => {
    if (!newTaskName.trim()) return;
    try {
      const added = await service.addTask(newTaskName, newTaskDate);
      setTasks([...tasks, added]);
      setNewTaskName('');
      setNewTaskDate('');
    } catch (error) {
      console.error("הוספת משימה נכשלה:", error);
    }
  };

  const removeTodo = async (id) => {
    try {
      await service.deleteTask(id);
      setTasks(tasks.filter(t => t.id !== id));
    } catch (error) {
      console.error("מחיקה נכשלה:", error);
    }
  };

  const toggleCompleted = async (task) => {
    try {
      const updated = await service.setCompleted(task.id, task.name, !task.isComplete, task.dueDate);
      setTasks(tasks.map(t => t.id === task.id ? updated : t));
    } catch (error) {
      console.error("עדכון משימה נכשל:", error);
    }
  };

  // --- תצוגה: מסך התחברות / הרשמה ---
  if (!isLoggedIn) {
    return (
      <div className="todo-container">
        <div className="todo-card">
          <h1 className="todo-title">{isRegistering ? "יצירת חשבון" : "כניסה למערכת"}</h1>
          <div className="input-group-vertical" style={{ display: 'flex', flexDirection: 'column', gap: '15px' }}>
            <input className="modern-input" placeholder="שם משתמש" value={username} onChange={e => setUsername(e.target.value)} />
            <input className="modern-input" type="password" placeholder="סיסמה" value={password} onChange={e => setPassword(e.target.value)} />
            {isRegistering ? (
              <>
                <button className="add-button register-btn" onClick={handleRegister}>הירשם עכשיו</button>
                <button className="logout-link" onClick={() => setIsRegistering(false)}>כבר יש לי חשבון? להתחברות</button>
              </>
            ) : (
              <>
                <button className="add-button" onClick={handleLogin}>התחברות</button>
                <button className="logout-link" onClick={() => setIsRegistering(true)}>אין לך חשבון? להרשמה</button>
              </>
            )}
          </div>
        </div>
      </div>
    );
  }

  // --- תצוגה: רשימת משימות (מוצג רק אם מחובר) ---
  return (
    <div className="todo-container">
      <div className="todo-card">
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
          <button onClick={handleLogout} className="logout-link" style={{ color: '#ef4444', margin: 0 }}>התנתק</button>
          <h1 className="todo-title" style={{ margin: 0 }}>המשימות שלי</h1>
        </div>

        {/* סטטיסטיקה */}
        <div className="stats-bar" style={{ background: '#f8fafc', padding: '10px', borderRadius: '8px', marginBottom: '15px', textAlign: 'center', border: '1px solid #e2e8f0' }}>
          סיימת <strong>{completedCount}</strong> מתוך <strong>{totalCount}</strong> משימות
          <div style={{ background: '#e2e8f0', height: '6px', borderRadius: '3px', marginTop: '8px', overflow: 'hidden' }}>
            <div style={{ background: '#3b82f6', height: '100%', width: `${(completedCount / (totalCount || 1)) * 100}%`, transition: 'width 0.4s' }}></div>
          </div>
        </div>

        {/* שדה חיפוש */}
        <input 
          className="modern-input" 
          style={{ marginBottom: '15px', width: '100%', boxSizing: 'border-box', border: '1px solid #cbd5e1' }}
          placeholder="🔎 חפש משימה לפי שם..." 
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
        />

        <div className="input-group" style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
          <input className="modern-input" style={{ flex: 2 }} value={newTaskName} onChange={(e) => setNewTaskName(e.target.value)} placeholder="מה התוכנית?" />
          <input type="date" className="modern-input" style={{ flex: 1, minWidth: '130px' }} value={newTaskDate} onChange={(e) => setNewTaskDate(e.target.value)} />
          <button className="add-button" onClick={createTodo}>הוספה</button>
        </div>

        <ul className="task-list">
          {tasks
            .filter(t => t.name.toLowerCase().includes(searchTerm.toLowerCase())) // פילטר חיפוש
            .sort((a, b) => {
              if (!a.dueDate) return 1;
              if (!b.dueDate) return -1;
              return new Date(a.dueDate) - new Date(b.dueDate);
            })
            .map(task => (
              <li key={task.id} className={`task-item ${task.isComplete ? 'completed' : ''}`}>
                <label className="checkbox-container">
                  <input type="checkbox" checked={task.isComplete || false} onChange={() => toggleCompleted(task)} />
                  <span className="checkmark"></span>
                </label>
                
                <div className="task-info" style={{ display: 'flex', flexDirection: 'column', flexGrow: 1, marginRight: '10px' }}>
                  <span className="task-text" style={{ textDecoration: task.isComplete ? 'line-through' : 'none' }}>{task.name}</span>
                  {task.dueDate && (
                    <span className="task-date" style={{ fontSize: '0.75rem', ...getDateStyle(task.dueDate) }}>
                      📅 {new Date(task.dueDate).toLocaleDateString('he-IL')}
                    </span>
                  )}
                </div>
                
                <button className="delete-btn" onClick={() => removeTodo(task.id)}>
                  <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2">
                    <path d="M3 6h18M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                  </svg>
                </button>
              </li>
            ))}
        </ul>

        {tasks.length === 0 && <p className="empty-state">אין משימות... זמן לנוח!</p>}
      </div>
    </div>
  );
}

export default App;