<template>
  <div class="auth-wrap">
    <div class="auth-card">
      <div class="auth-head">
        <h1 class="auth-title">Bem-vindo de volta</h1>
        <p class="auth-sub">Entra na tua conta para gerir eventos e ingressos.</p>
      </div>

      <form @submit.prevent="handleLogin">
        <div class="field">
          <label class="label" for="email">E-mail</label>
          <input id="email" v-model="email" type="email" class="input" placeholder="tu@exemplo.com" required />
        </div>
        <div class="field">
          <label class="label" for="password">Senha</label>
          <input id="password" v-model="password" type="password" class="input" placeholder="••••••••" required />
        </div>

        <p v-if="erro" class="alert alert-error">{{ erro }}</p>

        <button type="submit" class="btn btn-primary btn-block" :disabled="carregando">
          {{ carregando ? 'A entrar...' : 'Entrar' }}
        </button>
      </form>

      <p class="auth-foot">
        Ainda não tens conta?
        <RouterLink to="/register" class="link">Criar conta</RouterLink>
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const email = ref('')
const password = ref('')
const erro = ref('')
const carregando = ref(false)
const router = useRouter()
const authStore = useAuthStore()

const handleLogin = async () => {
  erro.value = ''
  carregando.value = true
  try {
    await authStore.login(email.value, password.value)
    router.push('/')
  } catch (err: any) {
    erro.value = err?.response?.data?.message || 'E-mail ou senha inválidos.'
  } finally {
    carregando.value = false
  }
}
</script>
