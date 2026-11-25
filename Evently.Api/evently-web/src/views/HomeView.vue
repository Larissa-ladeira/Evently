<template>
  <div>
    <section class="hero">
      <div class="hero-art" aria-hidden="true">
        <div class="orb orb-1"></div>
        <div class="orb orb-2"></div>
        <div class="orb orb-3"></div>
      </div>
      <div class="hero-content">
        <span class="eyebrow">
          <AppIcon name="sparkle" :size="14" />
          Plataforma de eventos
        </span>
        <h1 class="hero-title">
          Descubra e venda <span class="grad">experiências inesquecíveis</span>
        </h1>
        <p class="hero-sub">
          Crie eventos, venda ingressos, recolha inscrições personalizadas e faça check-in — tudo
          num só lugar.
        </p>

        <div class="searchbar">
          <AppIcon name="pin" :size="18" />
          <input
            v-model="search"
            type="search"
            placeholder="Procurar por evento, local ou artista..."
            @input="onSearchInput"
          />
          <button class="btn btn-primary btn-sm" @click="applyFilters">Buscar</button>
        </div>

        <div class="hero-actions">
          <RouterLink v-if="authStore.isOrganizer" to="/create-event" class="btn btn-ghost">
            <AppIcon name="plus" />
            Criar um evento
          </RouterLink>
          <a href="#eventos" class="btn btn-ghost">
            Ver todos
            <AppIcon name="arrow" />
          </a>
        </div>
      </div>
    </section>

    <section v-if="featured.length" class="section">
      <div class="section-head">
        <h2 class="section-title">Em destaque</h2>
        <span class="muted">Selecionados pela organização</span>
      </div>
      <div class="grid">
        <EventCard v-for="event in featured" :key="'f-' + event.id" :event="event" />
      </div>
    </section>

    <section id="eventos" class="section">
      <div class="section-head">
        <h2 class="section-title">Próximos eventos</h2>
        <span class="muted">{{ eventStore.events.length }} disponíveis</span>
      </div>

      <div class="filters">
        <div class="chips">
          <button
            v-for="cat in categories"
            :key="cat"
            class="chip"
            :class="{ active: filters.category === cat }"
            @click="selectCategory(cat)"
          >
            {{ cat }}
          </button>
        </div>
        <div class="filter-row">
          <select v-model="filters.sort" class="input filter-sort" @change="applyFilters">
            <option value="date_asc">Data (mais próximo)</option>
            <option value="date_desc">Data (mais distante)</option>
            <option value="price_asc">Preço (menor)</option>
            <option value="price_desc">Preço (maior)</option>
          </select>
          <input v-model.number="maxPrice" type="number" min="0" class="input filter-price" placeholder="Preço máx." @change="applyFilters" />
          <input v-model="fromDate" type="date" class="input filter-price" @change="applyFilters" />
        </div>
      </div>

      <div v-if="eventStore.loading" class="empty">A carregar eventos...</div>
      <div v-else-if="eventStore.events.length === 0" class="empty">
        Nenhum evento encontrado com estes filtros.
      </div>
      <div v-else class="grid">
        <EventCard v-for="event in eventStore.events" :key="event.id" :event="event" />
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { storeToRefs } from 'pinia'
import { useEventStore } from '../stores/events'
import type { EventFilters } from '../types'
import { useAuthStore } from '../stores/auth'
import AppIcon from '../components/AppIcon.vue'
import EventCard from '../components/EventCard.vue'

const eventStore = useEventStore()
const authStore = useAuthStore()
const { featured } = storeToRefs(eventStore)

const categories = ['Todos', 'Música', 'Tecnologia', 'Teatro', 'Desporto', 'Workshop', 'Gastronomia', 'Outros']

const search = ref('')
const maxPrice = ref<number | null>(null)
const fromDate = ref('')

const filters = reactive<EventFilters>({
  category: 'Todos',
  sort: 'date_asc',
})

let debounce: ReturnType<typeof setTimeout> | undefined

const applyFilters = () => {
  eventStore.fetchEvents({
    ...filters,
    q: search.value || undefined,
    minPrice: null,
    maxPrice: maxPrice.value,
    from: fromDate.value || null,
  })
}

const onSearchInput = () => {
  clearTimeout(debounce)
  debounce = setTimeout(applyFilters, 350)
}

const selectCategory = (cat: string) => {
  filters.category = cat
  applyFilters()
}

onMounted(() => {
  eventStore.fetchFeatured()
  applyFilters()
})
</script>
