import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5232', // שינינו מ-5000 ל-5232
});

// Interceptor שמוסיף את הטוקן לכל בקשה
api.interceptors.request.use(config => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

const service = {
  // --- פונקציות הזדהות ---
 login: async (username, password) => {
    // שימי לב ל-U ול-P הגדולות - זה חייב להתאים בדיוק ל-User.cs
    const res = await api.post('/login', { 
        Username: username, 
        Password: password 
    });
    localStorage.setItem('token', res.data.token); 
    return res.data;
},
register: async (username, password) => {
  return await api.post('/register', { Username: username, Password: password });
},

  logout: () => {
    localStorage.removeItem('token');
    window.location.reload();
  },

  // --- פונקציות משימות ---
  getTasks: async () => {
    const response = await api.get('/tasks');
    return response.data;
  },

addTask: async (name, dueDate) => {
  // משתמשים ב-api (עם ה-A הקטנה) כדי שהטוקן והכתובת יעבדו!
  const result = await api.post(`/tasks`, { 
    name: name, 
    isComplete: false,
    dueDate: dueDate ? dueDate : null 
  });
  return result.data;
},
setCompleted: async (id, name, isComplete, dueDate) => {
  const response = await api.put(`/tasks/${id}`, { 
    name: name, 
    isComplete: isComplete,
    dueDate: dueDate // שומר על התאריך הקיים
  });
  return response.data;
},

  deleteTask: async (id) => {
    await api.delete(`/tasks/${id}`);
  }
};

export default service;