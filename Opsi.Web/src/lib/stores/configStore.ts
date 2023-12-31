import { readable, writable } from "svelte/store";
import type Configuration from "../Models/Configuration/Configuration";

export const configStore = {};

export function setAsReadable(config: Configuration) {

    const obj = {};

    setProperties(obj, config);
    return obj;
}

function setProperties(current: any, value: any) {

    Object.entries(value).forEach(([key, value]) => {
        if (typeof value === "object") {
            current[key] = writable({});
            setProperties(current[key], value);
        } else {
            (current as any)[key] = readable(value);
        }
    });
}
