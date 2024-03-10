import { writable } from "svelte/store";
import type OpsiError from "../models/OpsiError";

function createErrorStore() {
  const arr: OpsiError[] = [];
  const { subscribe, set, update } = writable(arr);

  return {
    subscribe,
    add: (error: OpsiError) => update((errors: OpsiError[]) => {
      return [...errors, error];
    }),
    remove: (error: OpsiError) => update((errors: OpsiError[]) => {
      return errors.filter((e: Error) => e !== error);
    }),
    reset: () => set([])
  };
}

export const errors = createErrorStore();
