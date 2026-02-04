import React from 'react';
import { Outlet } from 'react-router-dom';

const App: React.FC = () => {
  return (
    <div id="app">
      <Outlet />
    </div>
  );
};

export default App;
