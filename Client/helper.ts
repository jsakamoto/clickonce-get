﻿namespace ClickOnceGet.Client.Helper {
    export function startClickOnce(url: string): void {
        const anchorElement = document.createElement('a');
        anchorElement.href = url;
        anchorElement.click();
        anchorElement.remove();
    }

    export function getCookie(key: string): string {
        const entry = document.cookie
            .split(';')
            .map(keyvalue => keyvalue.trim().split('='))
            .filter(keyvalue => decodeURIComponent(keyvalue[0]) === key)
            .pop();
        if (typeof (entry) === 'undefined') return '';
        return decodeURIComponent(entry[1]);
    }

    export function autofocus(): void {
        setTimeout(() => {
            const element = document.querySelector('input[autofocus]') as HTMLInputElement | null;
            if (element !== null) {
                element.focus();
                element.select();
            }
        }, 100);
    }
}