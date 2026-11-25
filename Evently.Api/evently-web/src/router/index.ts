import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import LoginView from '../views/LoginView.vue'
import RegisterView from '../views/RegisterView.vue'
import EventDetailView from '../views/EventDetailView.vue'
import CreateEventView from '../views/CreateEventView.vue'
import ManageEventView from '../views/ManageEventView.vue'
import MyTicketsView from '../views/MyTicketsView.vue'
import MyOrdersView from '../views/MyOrdersView.vue'
import OrderDetailView from '../views/OrderDetailView.vue'
import FavoritesView from '../views/FavoritesView.vue'
import DashboardView from '../views/DashboardView.vue'
import CheckInView from '../views/CheckInView.vue'
import ReportsView from '../views/ReportsView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', name: 'home', component: HomeView },
    { path: '/login', name: 'login', component: LoginView },
    { path: '/register', name: 'register', component: RegisterView },
    { path: '/events/:id', name: 'event-detail', component: EventDetailView },
    { path: '/events/:id/manage', name: 'event-manage', component: ManageEventView },
    { path: '/create-event', name: 'create-event', component: CreateEventView },
    { path: '/my-tickets', name: 'my-tickets', component: MyTicketsView },
    { path: '/orders', name: 'my-orders', component: MyOrdersView },
    { path: '/orders/:id', name: 'order-detail', component: OrderDetailView },
    { path: '/favorites', name: 'favorites', component: FavoritesView },
    { path: '/dashboard', name: 'dashboard', component: DashboardView },
    { path: '/checkin', name: 'checkin', component: CheckInView },
    { path: '/reports', name: 'reports', component: ReportsView },
  ],
  scrollBehavior() {
    return { top: 0 }
  },
})

export default router
