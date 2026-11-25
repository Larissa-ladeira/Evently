<template>
  <div class="auth-wrap">
    <div class="auth-card">
      <div class="auth-head">
        <h1 class="auth-title">Criar conta</h1>
        <p class="auth-sub">Começa a organizar eventos ou a comprar ingressos.</p>
      </div>

      <form @submit.prevent="handleRegister">
        <div class="field">
          <label class="label" for="name">Nome</label>
          <input id="name" v-model="name" type="text" class="input" placeholder="O teu nome" required />
        </div>
        <div class="field">
          <label class="label" for="email">E-mail</label>
          <input id="email" v-model="email" type="email" class="input" placeholder="tu@exemplo.com" required />
        </div>
        <div class="field">
          <label class="label" for="password">Senha</label>
          <input id="password" v-model="password" type="password" class="input" placeholder="••••••••" required />
        </div>
        <div class="field">
          <label class="label" for="role">Tipo de conta</label>
          <select id="role" v-model="role" class="input">
            <option value="Client">Comprador</option>
            <option value="Organizer">Organizador</option>
          </select>
        </div>

        <p v-if="erro" class="alert alert-error">{{ erro }}</p>

        <button type="submit" class="btn btn-primary btn-block" :disabled="carregando">
          {{ carregando ? 'A criar conta...' : 'Criar conta' }}
        </button>
      </form>

      <p class="auth-foot">
        Já tens conta?
        <RouterLink to="/login" class="link">Entrar</RouterLink>
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const name = ref('')
const email = ref('')
const password = ref('')
const role = ref('Client')
const erro = ref('')
const carregando = ref(false)
const router = useRouter()
const authStore = useAuthStore()

const handleRegister = async () => {
  erro.value = ''
  carregando.value = true
  try {
    await authStore.register(name.value, email.value, password.value, role.value)
    await authStore.login(email.value, password.value)
    router.push('/')
  } catch (err: any) {
    erro.value = err?.response?.data?.message || 'Erro ao criar conta.'
  } finally {
    carregando.value = false
  }
}
</script>
