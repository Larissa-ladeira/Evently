<script setup lang="ts">
import { onMounted, watch } from 'vue'
import { RouterLink, RouterView, useRouter } from 'vue-router'
import { useAuthStore } from './stores/auth'
import { useFavoriteStore } from './stores/favorites'
import AppIcon from './components/AppIcon.vue'

const authStore = useAuthStore()
const favoriteStore = useFavoriteStore()
const router = useRouter()

const logout = () => {
  authStore.logout()
  favoriteStore.$reset()
  router.push('/login')
}

const loadFavorites = () => {
  if (authStore.isAuthenticated) favoriteStore.load()
}

onMounted(loadFavorites)
watch(() => authStore.isAuthenticated, loadFavorites)
</script>

<template>
  <div class="app">
    <header class="navbar">
      <div class="nav-inner">
        <RouterLink to="/" class="brand">
          <span class="brand-mark">E</span>
          <span>Evently</span>
        </RouterLink>

        <nav class="nav-links">
          <RouterLink to="/" class="nav-link">Eventos</RouterLink>
          <RouterLink v-if="authStore.isOrganizer" to="/create-event" class="nav-link">
            Criar evento
          </RouterLink>
          <RouterLink v-if="authStore.isOrganizer" to="/dashboard" class="nav-link">
            Dashboard
          </RouterLink>
          <RouterLink v-if="authStore.isStaff" to="/checkin" class="nav-link">Check-in</RouterLink>
          <RouterLink v-if="authStore.isAuthenticated" to="/orders" class="nav-link">
            Pedidos
          </RouterLink>
          <RouterLink v-if="authStore.isAuthenticated" to="/my-tickets" class="nav-link">
            Ingressos
          </RouterLink>
          <RouterLink v-if="authStore.isAuthenticated" to="/favorites" class="nav-link fav-link">
            <AppIcon :name="'sparkle'" :size="15" />
            Favoritos
            <span v-if="favoriteStore.count" class="fav-count">{{ favoriteStore.count }}</span>
          </RouterLink>
        </nav>

        <div class="nav-actions">
          <template v-if="authStore.isAuthenticated">
            <div class="user-chip">
              <span class="user-avatar">{{ authStore.user?.name?.charAt(0) }}</span>
              <span class="user-meta">
                <span class="user-name">{{ authStore.user?.name }}</span>
                <span class="user-role">{{ authStore.user?.role }}</span>
              </span>
            </div>
            <button class="btn btn-ghost btn-sm" @click="logout">Sair</button>
          </template>
          <template v-else>
            <RouterLink to="/login" class="btn btn-ghost btn-sm">Entrar</RouterLink>
            <RouterLink to="/register" class="btn btn-primary btn-sm">Criar conta</RouterLink>
          </template>
        </div>
      </div>
    </header>

    <main class="main">
      <RouterView />
    </main>

    <footer class="footer">Evently · Plataforma de gestão de eventos e ingressos</footer>
  </div>
</template>
