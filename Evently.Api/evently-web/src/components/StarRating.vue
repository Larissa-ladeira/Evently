<script setup lang="ts">
const props = defineProps<{ value: number | null | undefined; count?: number; size?: number }>()

const full = () => Math.round(props.value ?? 0)
</script>

<template>
  <span class="stars" :title="value ? `${value} de 5` : 'Sem avaliações'">
    <svg
      v-for="i in 5"
      :key="i"
      :width="size ?? 15"
      :height="size ?? 15"
      viewBox="0 0 24 24"
      :class="i <= full() ? 'star-on' : 'star-off'"
    >
      <path
        d="M12 2.5l2.9 6 6.6.9-4.8 4.6 1.2 6.5L12 17.9 6.1 20.5l1.2-6.5L2.5 9.4l6.6-.9z"
        fill="currentColor"
      />
    </svg>
    <span v-if="count !== undefined" class="stars-count">{{ value ? value : '—' }} ({{ count }})</span>
  </span>
</template>
