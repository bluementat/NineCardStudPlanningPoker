import { createApp } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'
import App from './App.vue'
import Home from './views/Home.vue'
import Session from './views/Session.vue'
import './assets/style.css'

const routes = [
  { path: '/', component: Home },
  { path: '/session/:pin?', component: Session, props: true }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

const app = createApp(App)
app.use(router)
app.mount('#app')
