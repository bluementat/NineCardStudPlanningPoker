import React from 'react';
import { Outlet } from 'react-router-dom';

const App: React.FC = () => {
  return (
    <div id="app">
      <main data-testid="app-main">
        <Outlet />
      </main>
    </div>
  );
};

export default App;
