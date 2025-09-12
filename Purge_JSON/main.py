import os, json, sys
import re

if __name__ == "__main__":
    #json_str = json.dumps(result['Children'], indent=2)
    all_node = []
    all_documents = []
    with open("result.json", "r", encoding="utf-8") as out_stream:
        all_node = json.load(out_stream)
    for x in all_node:
        second_step = x['Children']
        for y in second_step:
            third_step = y['Children']#list of document
            for counter in range(len(third_step)):
                document = third_step[counter]['Name']
                pattern = r"-(\d{3})-"
                match = re.search(pattern, document)
                if match:
                    split_index = match.end() - 1
                    #idx = match.start()  # محل شروع این بخش
                    document_number = document[:split_index]   # قبل از این بخش
                    document_name = document[split_index:]   # از این بخش به بعد
                    if str(document_name).startswith('-'):
                        document_name=document_name[1:]
                    children = third_step[counter]['Children']
                    revisions = [item['Name'] for item in children]
                    if '1389-ar' in str(document_number).lower():
                        all_documents.append({
                            "Document_name" : document_name,
                            "Document_number" : document_number ,
                            "Dicipline" : x['Name'],
                            "Document_type" : y['Name'],
                            "Revisions" : revisions,
                        })
                else:
                    print(document = third_step[counter]['Name'])
    json_str = json.dumps(all_documents, indent=2)
    with open("database.json", "w", encoding="utf-8") as out_stream:
        out_stream.write(json_str)
    
