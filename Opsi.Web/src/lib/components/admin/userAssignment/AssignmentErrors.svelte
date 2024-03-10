<script lang="ts">
  import type AssignmentError from "./AssignmentError";

  export let assignmentErrors: AssignmentError[] = [];
</script>

{#if assignmentErrors.length > 0}
    <div>
        <ul>
            {#each assignmentErrors as assignmentError (assignmentError.resourceFullName)}
                <li>
                    <span>{assignmentError.resourceFullName}</span>
                    <span>{assignmentError.errorMessage.replace(/\\n/g, "<br />")}</span>
                </li>
            {/each}
        </ul>
    </div>
{/if}

<style lang="scss">
    div {
        margin-bottom: 2rem;
    }

    li {
        &:not(:last-child) {
            margin-bottom: 1rem;
        }

        & > span {
            display: block;

            &:last-child {
                color: red;
            }
        }
    }
</style>