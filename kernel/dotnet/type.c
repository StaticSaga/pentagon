#include "type.h"
#include "types.h"

#include <mem/malloc.h>
#include <util/string.h>
#include <dotnet/field_info.h>

#include <stdalign.h>

bool type_has_field(type_t type, field_info_t info) {
    // for value types
    if (type->is_by_ref) {
        type = type->element_type;
    }

    do {
        if (info->declaring_type == type) {
            return true;
        }
        type = type->base_type;
    } while (type != NULL);
    return false;
}

type_t make_array_type(type_t type) {
    if (type->by_ref_type != NULL) {
        return type->by_ref_type;
    }

    // slow path when it needs allocation
    spinlock_lock(&type->array_type_lock);
    if (type->array_type != NULL) {
        spinlock_unlock(&type->array_type_lock);
        return type->array_type;
    }

    type_t new_type = malloc(sizeof(struct type));
    memset(new_type, 0, sizeof(struct type));
    *new_type = *g_array;
    new_type->is_array = true;
    new_type->element_type = type;

    type->array_type = new_type;

    spinlock_unlock(&type->array_type_lock);
    return new_type;
}

type_t make_by_ref_type(type_t type) {
    // fast path
    if (type->array_type != NULL) {
        return type->array_type;
    }

    // slow path when it needs allocation
    spinlock_lock(&type->by_ref_type_lock);
    if (type->array_type != NULL) {
        spinlock_unlock(&type->by_ref_type_lock);
        return type->array_type;
    }

    type_t new_type = malloc(sizeof(struct type));
    memset(new_type, 0, sizeof(struct type));
    new_type->assembly = type->assembly;
    new_type->stack_alignment = sizeof(void*);
    new_type->stack_size = sizeof(void*);
    new_type->managed_alignment = type->stack_alignment;
    new_type->managed_size = type->stack_size;
    new_type->is_by_ref = true;
    new_type->element_type = type;

    type->array_type = new_type;

    spinlock_unlock(&type->by_ref_type_lock);
    return new_type;
}

type_t make_pointer_type(type_t type) {
    // fast path
    if (type->pointer_type != NULL) {
        return type->pointer_type;
    }

    // slow path when it needs allocation
    spinlock_lock(&type->pointer_type_lock);
    if (type->pointer_type != NULL) {
        spinlock_unlock(&type->pointer_type_lock);
        return type->pointer_type;
    }

    type_t new_type = malloc(sizeof(struct type));
    memset(new_type, 0, sizeof(struct type));
    new_type->assembly = type->assembly;
    new_type->stack_alignment = sizeof(void*);
    new_type->stack_size = sizeof(void*);
    new_type->managed_alignment = type->stack_alignment;
    new_type->managed_size = type->stack_size;
    new_type->is_pointer = true;
    new_type->is_value_type = true;
    new_type->is_primitive = true;
    new_type->element_type = type;

    type->pointer_type = new_type;

    spinlock_unlock(&type->pointer_type_lock);
    return new_type;
}

void type_full_name(type_t type, buffer_t* buffer) {
    if (type->is_by_ref) {
        type_full_name(type->element_type, buffer);
        bputc('&', buffer);
    } else if (type->is_pointer) {
        type_full_name(type->element_type, buffer);
        bputc('*', buffer);
    } else if (type->is_array) {
        type_full_name(type->element_type, buffer);
        bputc('[', buffer);
        bputc(']', buffer);
    } else {
        if (type->declaring_type != NULL) {
            type_full_name(type->declaring_type, buffer);
            bputc('+', buffer);
        }

        if (type->namespace != NULL) {
            bprintf(buffer, "%s.", type->namespace);
        }

        bprintf(buffer, "%s", type->name);
    }
}
