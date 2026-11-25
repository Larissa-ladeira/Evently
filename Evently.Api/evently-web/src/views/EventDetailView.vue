<template>
  <div v-if="currentEvent">
    <RouterLink to="/" class="link">&larr; Voltar aos eventos</RouterLink>

    <div class="detail-hero detail-hero-img" :class="coverClass" :style="coverStyle">
      <span class="badge badge-primary badge-glass">{{ currentEvent.category || 'Outros' }}</span>
      <h1 class="page-title" style="font-size: clamp(26px, 4vw, 40px); margin-top: 12px">
        {{ currentEvent.title }}
      </h1>
      <div class="detail-meta">
        <span><AppIcon name="calendar" :size="16" /> {{ new Date(currentEvent.date).toLocaleString() }}</span>
        <span><AppIcon name="pin" :size="16" /> {{ currentEvent.location }}</span>
        <span v-if="currentEvent.organizerName"><AppIcon name="user" :size="16" /> {{ currentEvent.organizerName }}</span>
        <StarRating :value="currentEvent.rating" :count="currentEvent.reviewCount" />
      </div>
      <div class="hero-actions">
        <button class="btn btn-ghost btn-sm" @click="toggleFav">
          <AppIcon :name="isFav ? 'check' : 'sparkle'" :size="15" />
          {{ isFav ? 'Nos favoritos' : 'Favoritar' }}
        </button>
        <button class="btn btn-ghost btn-sm" @click="share">
          <AppIcon name="arrow" :size="15" />
          Partilhar
        </button>
      </div>
    </div>

    <div class="detail-grid">
      <div>
        <div class="card">
          <h2 class="section-title" style="font-size: 20px">Sobre o evento</h2>
          <p class="event-desc mt-8" style="color: var(--text-soft); -webkit-line-clamp: unset">
            {{ currentEvent.description }}
          </p>

          <template v-if="currentForm && currentForm.fields.length">
            <h2 class="section-title mt-24" style="font-size: 20px">Formulário de inscrição</h2>
            <div class="mt-16">
              <div v-for="field in currentForm.fields" :key="field.name" class="field">
                <label class="label">{{ field.label }}<span v-if="field.required"> *</span></label>
                <input
                  v-if="field.type !== 'select' && field.type !== 'checkbox'"
                  :type="inputType(field.type)"
                  class="input"
                  v-model="answers[field.name]"
                />
                <select v-else-if="field.type === 'select'" class="input" v-model="answers[field.name]">
                  <option value="">Selecione...</option>
                  <option v-for="opt in field.options || []" :key="opt" :value="opt">{{ opt }}</option>
                </select>
                <label v-else class="inline" style="font-weight: 500">
                  <input
                    type="checkbox"
                    :checked="answers[field.name] === 'true'"
                    @change="answers[field.name] = ($event.target as HTMLInputElement).checked ? 'true' : ''"
                  />
                  Sim
                </label>
              </div>
            </div>
          </template>
        </div>

        <div class="card mt-16">
          <div class="section-head" style="margin-bottom: 12px">
            <h2 class="section-title" style="font-size: 20px">Avaliações</h2>
            <StarRating :value="reviews.average" :count="reviews.count" />
          </div>

          <form v-if="authStore.isAuthenticated" class="review-form" @submit.prevent="enviarReview">
            <div class="field" style="margin: 0">
              <label class="label">A tua classificação</label>
              <div class="rate-input">
                <button
                  v-for="n in 5"
                  :key="n"
                  type="button"
                  class="rate-star"
                  :class="{ on: n <= myRating }"
                  @click="myRating = n"
                >
                  <svg width="24" height="24" viewBox="0 0 24 24">
                    <path
                      d="M12 2.5l2.9 6 6.6.9-4.8 4.6 1.2 6.5L12 17.9 6.1 20.5l1.2-6.5L2.5 9.4l6.6-.9z"
                      fill="currentColor"
                    />
                  </svg>
                </button>
              </div>
            </div>
            <textarea v-model="myComment" placeholder="Conta-nos como foi a tua experiência..." class="mt-8"></textarea>
            <p v-if="reviewMsg" class="alert" :class="reviewOk ? 'alert-success' : 'alert-error'">{{ reviewMsg }}</p>
            <button type="submit" class="btn btn-primary btn-sm mt-8">Enviar avaliação</button>
          </form>
          <p v-else class="muted mt-8">
            <RouterLink to="/login" class="link">Entra</RouterLink> para avaliar este evento.
          </p>

          <div v-if="reviews.reviews.length === 0" class="muted mt-16">Ainda sem avaliações.</div>
          <div v-else class="reviews mt-16">
            <div v-for="r in reviews.reviews" :key="r.id" class="review">
              <div class="review-head">
                <span class="user-avatar">{{ r.userName.charAt(0) }}</span>
                <strong>{{ r.userName }}</strong>
                <StarRating :value="r.rating" :size="13" />
                <span class="muted" style="margin-left: auto">{{ new Date(r.createdAt).toLocaleDateString() }}</span>
              </div>
              <p v-if="r.comment" class="review-text">{{ r.comment }}</p>
            </div>
          </div>
        </div>
      </div>

      <aside class="card purchase-card">
        <h2 class="section-title" style="font-size: 20px">Ingressos</h2>
        <p v-if="batches.length === 0" class="muted mt-8">Nenhum lote disponível.</p>
        <template v-else>
          <div v-for="batch in batches" :key="batch.id" class="batch" :class="{ 'batch-off': !batch.onSale || batch.soldOut }">
            <div>
              <div class="batch-name">{{ batch.name }}</div>
              <small v-if="batch.soldOut" class="text-danger">Esgotado</small>
              <small v-else-if="!batch.onSale">Fora do período de venda</small>
              <small v-else>{{ batch.available }} disponíveis</small>
            </div>
            <div class="inline">
              <span class="batch-price">R$ {{ batch.price.toFixed(2) }}</span>
              <input
                class="input qty"
                type="number"
                min="0"
                :max="batch.available"
                :disabled="batch.soldOut || !batch.onSale"
                v-model.number="quantities[batch.id]"
                @input="onQty"
              />
            </div>
          </div>

          <div class="coupon-row mt-16">
            <input v-model="couponCode" class="input" placeholder="Código de cupom" />
            <button class="btn btn-ghost btn-sm" :disabled="!couponCode" @click="aplicarCupom">Aplicar</button>
          </div>
          <p v-if="couponMsg" class="alert" :class="couponOk ? 'alert-success' : 'alert-error'" style="padding: 8px 12px">
            {{ couponMsg }}
          </p>

          <div class="summary mt-16">
            <div class="summary-row"><span>Subtotal</span><span>R$ {{ subtotal.toFixed(2) }}</span></div>
            <div v-if="discount > 0" class="summary-row text-success">
              <span>Desconto {{ appliedCoupon }}</span><span>- R$ {{ discount.toFixed(2) }}</span>
            </div>
            <div class="summary-row summary-total"><span>Total</span><span>R$ {{ total.toFixed(2) }}</span></div>
          </div>

          <p v-if="mensagem" class="alert" :class="sucesso ? 'alert-success' : 'alert-error'">{{ mensagem }}</p>

          <button class="btn btn-primary btn-block mt-16" @click="comprar" :disabled="comprando || currentEvent.soldOut">
            <AppIcon name="ticket" />
            {{ currentEvent.soldOut ? 'Esgotado' : comprando ? 'A processar...' : 'Comprar ingresso' }}
          </button>
        </template>
      </aside>
    </div>
  </div>

  <div v-else class="empty">A carregar evento...</div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { storeToRefs } from 'pinia'
import { useEventStore } from '../stores/events'
import type { Review } from '../types'
import { useAuthStore } from '../stores/auth'
import { useFavoriteStore } from '../stores/favorites'
import AppIcon from '../components/AppIcon.vue'
import StarRating from '../components/StarRating.vue'

const route = useRoute()
const router = useRouter()
const eventStore = useEventStore()
const authStore = useAuthStore()
const favoriteStore = useFavoriteStore()

const { currentEvent, batches, currentForm } = storeToRefs(eventStore)

const quantities = reactive<Record<string, number>>({})
const answers = reactive<Record<string, string>>({})
const comprando = ref(false)
const sucesso = ref(false)
const mensagem = ref('')

const couponCode = ref('')
const appliedCoupon = ref('')
const discount = ref(0)
const couponMsg = ref('')
const couponOk = ref(false)

const reviews = ref<{ average: number; count: number; reviews: Review[] }>({
  average: 0,
  count: 0,
  reviews: [],
})
const myRating = ref(5)
const myComment = ref('')
const reviewMsg = ref('')
const reviewOk = ref(false)

const coverClass = computed(() => {
  const cats: Record<string, string> = {
    Música: 'cover-0',
    Tecnologia: 'cover-1',
    Teatro: 'cover-2',
    Desporto: 'cover-3',
    Workshop: 'cover-4',
    Gastronomia: 'cover-5',
  }
  return currentEvent.value?.imageUrl ? '' : cats[currentEvent.value?.category ?? ''] ?? 'cover-6'
})

const coverStyle = computed(() =>
  currentEvent.value?.imageUrl ? { backgroundImage: `url(${currentEvent.value.imageUrl})` } : {}
)

const isFav = computed(() => (currentEvent.value ? favoriteStore.isFavorite(currentEvent.value.id) : false))

const inputType = (type: string) => {
  if (type === 'number') return 'number'
  if (type === 'email') return 'email'
  if (type === 'date') return 'date'
  return 'text'
}

const subtotal = computed(() =>
  batches.value.reduce((sum, b) => sum + (quantities[b.id] || 0) * b.price, 0)
)
const total = computed(() => Math.max(0, subtotal.value - discount.value))

const onQty = () => {
  discount.value = 0
  appliedCoupon.value = ''
  couponMsg.value = ''
}

const aplicarCupom = async () => {
  couponOk.value = false
  couponMsg.value = ''
  try {
    const res = await eventStore.applyCoupon(route.params.id as string, couponCode.value, subtotal.value)
    discount.value = res.discount
    appliedCoupon.value = res.code
    couponOk.value = true
    couponMsg.value = `${res.message} (-R$ ${res.discount.toFixed(2)})`
  } catch (err: any) {
    discount.value = 0
    appliedCoupon.value = ''
    couponMsg.value = err?.response?.data?.message || 'Cupom inválido.'
  }
}

const toggleFav = async () => {
  if (!authStore.isAuthenticated) {
    router.push('/login')
    return
  }
  if (currentEvent.value) await favoriteStore.toggle(currentEvent.value.id)
}

const share = async () => {
  const url = window.location.href
  if (navigator.share) {
    try {
      await navigator.share({ title: currentEvent.value?.title, url })
      return
    } catch {
      /* cancelado */
    }
  }
  await navigator.clipboard.writeText(url)
  mensagem.value = 'Link copiado para a área de transferência!'
  sucesso.value = true
}

const enviarReview = async () => {
  reviewOk.value = false
  reviewMsg.value = ''
  try {
    await eventStore.submitReview(route.params.id as string, myRating.value, myComment.value)
    reviewOk.value = true
    reviewMsg.value = 'Avaliação registada. Obrigado!'
    myComment.value = ''
    reviews.value = await eventStore.fetchReviews(route.params.id as string)
  } catch (err: any) {
    reviewMsg.value = err?.response?.data?.message || 'Não foi possível enviar a avaliação.'
  }
}

const comprar = async () => {
  if (!authStore.isAuthenticated) {
    router.push('/login')
    return
  }

  const items = batches.value
    .map((b) => ({ ticketTypeId: b.id, quantity: quantities[b.id] || 0 }))
    .filter((i) => i.quantity > 0)

  sucesso.value = false
  if (items.length === 0) {
    mensagem.value = 'Selecione pelo menos um ingresso.'
    return
  }

  comprando.value = true
  mensagem.value = ''
  try {
    const order = await eventStore.checkout(
      route.params.id as string,
      items,
      answers,
      appliedCoupon.value || undefined
    )
    sucesso.value = true
    router.push('/orders/' + order.orderId)
  } catch (err: any) {
    mensagem.value = err?.response?.data?.message || 'Erro ao realizar compra.'
  } finally {
    comprando.value = false
  }
}

onMounted(async () => {
  const id = route.params.id as string
  await Promise.all([
    eventStore.fetchEvent(id),
    eventStore.fetchBatches(id),
    eventStore.fetchForm(id),
    (async () => {
      reviews.value = await eventStore.fetchReviews(id)
    })(),
  ])
  if (authStore.isAuthenticated) favoriteStore.load()
})
</script>
