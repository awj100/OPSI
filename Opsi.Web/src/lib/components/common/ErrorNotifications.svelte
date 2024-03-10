<script lang="ts">
  import { ToastNotification } from "carbon-components-svelte";
  import { errors } from "../../stores/errorsStore";

  const ErrorDisplayTimeoutMs = 5000;
</script>

{#each $errors as error (error.uniqueId)}
  <ToastNotification
    caption={new Date().toLocaleString()}
    fullWidth={true}
    subtitle={error.message}
    timeout={ErrorDisplayTimeoutMs}
    title={error.displayTitle}
    on:close={(e) => {
      if (!e.detail.timeout) {
        errors.remove(error);
      }
    }} />
{/each}