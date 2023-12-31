import Configuration from "../Models/Configuration/Configuration";

const configKey: string = "opsi.config";

export function loadConfiguration(): Configuration {
    const strConfig = localStorage.getItem(configKey);
    if (strConfig === null) {
        return getDefault();
    }

    try {
        return JSON.parse(strConfig) as Configuration ;
    } catch {
        localStorage.removeItem(configKey);
        console.error("The stored configuration is invalid. It has been removed.");
        return getDefault();
    }
}

export function setConfiguration(configuration: Configuration): void {
    localStorage.setItem(configKey, JSON.stringify(configuration));
}

function getDefault(): Configuration {
    return new Configuration();
}
